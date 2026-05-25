using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using VeterinaryClinic.Shared.ContextAccessor;

namespace VeterinaryClinic.Business
{
    public class ProcessAppointmentCommand : IRequest<object>
    {
        public int AppointmentId { get; }
        public ProcessAppointmentModel Model { get; }

        public ProcessAppointmentCommand(int appointmentId, ProcessAppointmentModel model)
        {
            AppointmentId = appointmentId;
            Model = model;
        }

        public class Handler : IRequestHandler<ProcessAppointmentCommand, object>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly IContextAccessor _contextAccessor;
            private readonly IStringLocalizer<ProcessAppointmentCommand> _localizer;
            private readonly ICacheService _cacheService;
            private readonly IAppointmentStateMachine _appointmentStateMachine;
            private readonly IMediator _mediator;

            public Handler(
                VeterinaryClinicDataContext dataContext,
                Func<IContextAccessor> contextAccessorFactory,
                IStringLocalizer<ProcessAppointmentCommand> localizer,
                ICacheService cacheService,
                IAppointmentStateMachine appointmentStateMachine,
                IMediator mediator)
            {
                _dataContext = dataContext;
                _contextAccessor = contextAccessorFactory();
                _localizer = localizer;
                _cacheService = cacheService;
                _appointmentStateMachine = appointmentStateMachine;
                _mediator = mediator;
            }

            public async Task<object> Handle(ProcessAppointmentCommand request, CancellationToken cancellationToken)
            {
                if (request.Model == null)
                {
                    throw new ArgumentException(_localizer["appointment.action.required"]);
                }

                var appointment = await _dataContext.VcAppointments.FindAsync(new object[] { request.AppointmentId }, cancellationToken);
                if (appointment == null || !appointment.IsActive)
                {
                    throw new KeyNotFoundException(_localizer["appointment.not_found"]);
                }

                if (string.IsNullOrWhiteSpace(request.Model.Action) ||
                    !Enum.TryParse<AppointmentAction>(request.Model.Action, true, out var action))
                {
                    throw new ArgumentException(_localizer["appointment.action.invalid"]);
                }

                ValidatePermission(appointment, action);

                if (action == AppointmentAction.BANK_TRANSFER)
                {
                    return await _mediator.Send(
                        new CreateVnPayPaymentCommand(new CreateVnPayPaymentModel
                        {
                            AppointmentId = appointment.Id
                        }),
                        cancellationToken);
                }

                _appointmentStateMachine.Apply(appointment, action, request.Model.CancelReason);

                // Đảm bảo StateName luôn được cập nhật đúng chính tả từ StateMachine
                appointment.StateName = _appointmentStateMachine.GetStateDisplayName(Enum.Parse<AppointmentStatus>(appointment.State));

                if (action == AppointmentAction.CASH_PAYMENT)
                {
                    await ApplyCashPayment(appointment, cancellationToken);
                }

                appointment.ModifiedDate = DateTime.UtcNow;
                appointment.ModifiedUserId = _contextAccessor.UserId;
                appointment.ModifiedUserName = _contextAccessor.UserName;

                await _dataContext.SaveChangesAsync(cancellationToken);
                _cacheService.Remove(AppointmentConstant.BuildCacheKey());

                Log.Information(
                    "Appointment {AppointmentId} processed with action {Action} by user {UserId}",
                    appointment.Id,
                    request.Model.Action,
                    _contextAccessor.UserId);

                return Unit.Value;
            }

            private void ValidatePermission(VcAppointments appointment, AppointmentAction action)
            {
                var role = _contextAccessor.Role;
                var userId = _contextAccessor.UserId;

                var isAdmin = role == Role.ADMIN.ToString();
                var isDoctor = role == Role.DOCTOR.ToString();
                var isCustomer = role == Role.CUSTOMER.ToString();
                var isReceptionist = role == Role.RECEPTIONIST.ToString();

                var allowed = action switch
                {
                    AppointmentAction.CONFIRM => isAdmin || (isDoctor && appointment.DoctorId == userId),
                    AppointmentAction.REJECT => isAdmin,
                    AppointmentAction.CUSTOMER_CANCEL => isCustomer && appointment.CustomerId == userId,
                    AppointmentAction.START_CONSULTATION => isDoctor && appointment.DoctorId == userId,
                    AppointmentAction.MARK_NO_SHOW => isAdmin || (isDoctor && appointment.DoctorId == userId),
                    AppointmentAction.COMPLETE_CONSULTATION => isDoctor && appointment.DoctorId == userId,
                    AppointmentAction.COMPLETE_PAYMENT => isAdmin || isReceptionist,
                    AppointmentAction.BANK_TRANSFER => isAdmin || isReceptionist || (isCustomer && appointment.CustomerId == userId),
                    AppointmentAction.CASH_PAYMENT => isAdmin || isReceptionist,
                    _ => false
                };

                if (!allowed)
                {
                    throw new UnauthorizedAccessException(_localizer["user.unauthorized"]);
                }
            }

            private async Task ApplyCashPayment(VcAppointments appointment, CancellationToken cancellationToken)
            {
                var pendingBillingItems = await (
                    from invoice in _dataContext.VcInvoices
                    join appt in _dataContext.VcAppointments on invoice.AppointmentId equals appt.Id
                    where invoice.IsActive &&
                          appt.IsActive &&
                          appt.CustomerId == appointment.CustomerId &&
                          invoice.Status != PaymentStatus.SUCCESS.ToString() &&
                          appt.State == AppointmentStatus.PAYMENT_PENDING.ToString()
                    select new { Invoice = invoice, Appointment = appt })
                    .ToListAsync(cancellationToken);

                if (!pendingBillingItems.Any())
                {
                    throw new ArgumentException(_localizer["invoice.not_found"]);
                }

                if (pendingBillingItems.Any(x => x.Invoice.TotalAmount <= 0))
                {
                    throw new ArgumentException(_localizer["invoice.amount.invalid"]);
                }

                var paymentCode = GenerateCodeUtils.GenerateUserCode("PAY");
                var paidDate = DateTime.UtcNow;
                var payments = new List<VcPayments>();

                foreach (var item in pendingBillingItems)
                {
                    item.Invoice.Status = PaymentStatus.SUCCESS.ToString();
                    item.Invoice.PaidDate = paidDate;

                    item.Appointment.State = AppointmentStatus.COMPLETED.ToString();
                    item.Appointment.StateName = _appointmentStateMachine.GetStateDisplayName(AppointmentStatus.COMPLETED);
                    item.Appointment.IsFinalState = true;
                    item.Appointment.ModifiedDate = paidDate;
                    item.Appointment.ModifiedUserId = _contextAccessor.UserId;
                    item.Appointment.ModifiedUserName = _contextAccessor.UserName;

                    payments.Add(new VcPayments
                    {
                        InvoiceId = item.Invoice.Id,
                        Code = paymentCode,
                        PaymentMethod = PaymentMethod.CASH.ToString(),
                        PaymentStatus = PaymentStatus.SUCCESS.ToString(),
                        Amount = item.Invoice.TotalAmount,
                        GatewayTransactionId = null,
                        ResponseCode = null,
                        GatewayResponse = null,
                        PaymentDate = paidDate,
                        IsActive = true,
                        Order = 0,
                        CreatedDate = paidDate,
                        CreatedUserId = _contextAccessor.UserId,
                        CreatedUserName = _contextAccessor.UserName
                    });
                }

                await _dataContext.VcPayments.AddRangeAsync(payments, cancellationToken);
            }
        }
    }
}
