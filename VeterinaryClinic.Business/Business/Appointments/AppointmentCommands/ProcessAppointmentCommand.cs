using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using VeterinaryClinic.Shared.ContextAccessor;
using VeterinaryClinic.Business.Services;
using VeterinaryClinic.Business.Models;

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
            private readonly INotificationService _notificationService;

            public Handler(
                VeterinaryClinicDataContext dataContext,
                Func<IContextAccessor> contextAccessorFactory,
                IStringLocalizer<ProcessAppointmentCommand> localizer,
                ICacheService cacheService,
                IAppointmentStateMachine appointmentStateMachine,
                IMediator mediator,
                INotificationService notificationService)
            {
                _dataContext = dataContext;
                _contextAccessor = contextAccessorFactory();
                _localizer = localizer;
                _cacheService = cacheService;
                _appointmentStateMachine = appointmentStateMachine;
                _mediator = mediator;
                _notificationService = notificationService;
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

                appointment.StateName = _appointmentStateMachine.GetStateDisplayName(Enum.Parse<AppointmentStatus>(appointment.State));

                if (action == AppointmentAction.CASH_PAYMENT)
                {
                    await ApplyCashPayment(appointment, cancellationToken);
                }

                appointment.ModifiedDate = DateTime.UtcNow;
                appointment.ModifiedUserId = _contextAccessor.UserId;
                appointment.ModifiedUserName = _contextAccessor.UserName;

                await _dataContext.SaveChangesAsync(cancellationToken);
                
                await SendNotifications(appointment, action);

                _cacheService.Remove(AppointmentConstant.BuildCacheKey());

                Log.Information(
                    "Appointment {AppointmentId} processed with action {Action} by user {UserId}",
                    appointment.Id,
                    request.Model.Action,
                    _contextAccessor.UserId);

                return Unit.Value;
            }

            private async Task SendNotifications(VcAppointments appointment, AppointmentAction action)
            {
                NotificationModel? customerNotification = null;
                NotificationModel? doctorNotification = null;

                switch (action)
                {
                    case AppointmentAction.CUSTOMER_CANCEL:
                        doctorNotification = new NotificationModel
                        {
                            UserId = appointment.DoctorId,
                            Title = "Lịch hẹn đã bị hủy",
                            Message = $"Lịch hẹn mã {appointment.Code} đã bị khách hàng hủy.",
                            Type = NotificationType.MESSAGE.ToString(),
                            RelatedEntityId = appointment.Id,
                            RelatedEntityType = RelatedEntityType.Appointment.ToString()
                        };
                        break;

                    case AppointmentAction.COMPLETE_CONSULTATION:
                        customerNotification = new NotificationModel
                        {
                            UserId = appointment.CustomerId,
                            Title = "Buổi khám đã hoàn tất",
                            Message = $"Buổi khám cho lịch hẹn {appointment.Code} đã hoàn tất. Vui lòng tiến hành thanh toán.",
                            Type = NotificationType.MESSAGE.ToString(),
                            RelatedEntityId = appointment.Id,
                            RelatedEntityType = RelatedEntityType.Appointment.ToString()
                        };
                        break;

                    case AppointmentAction.CASH_PAYMENT:
                    case AppointmentAction.BANK_TRANSFER:
                         customerNotification = new NotificationModel
                        {
                            UserId = appointment.CustomerId,
                            Title = "Thanh toán thành công",
                            Message = $"Thanh toán cho lịch hẹn {appointment.Code} đã được ghi nhận thành công.",
                            Type = NotificationType.MESSAGE.ToString(),
                            RelatedEntityId = appointment.Id,
                            RelatedEntityType = RelatedEntityType.Appointment.ToString()
                        };
                        break;
                    
                    case AppointmentAction.MARK_NO_SHOW:
                        customerNotification = new NotificationModel
                        {
                            UserId = appointment.CustomerId,
                            Title = "Lịch hẹn bị đánh dấu không đến",
                            Message = $"Bạn đã không đến lịch hẹn {appointment.Code} và đã bị ghi nhận trong hệ thống.",
                            Type = NotificationType.MESSAGE.ToString(),
                            RelatedEntityId = appointment.Id,
                            RelatedEntityType = RelatedEntityType.Appointment.ToString()
                        };
                        break;
                }

                if (customerNotification != null)
                {
                    await _notificationService.SendAndSaveNotificationAsync(customerNotification);
                }
                if (doctorNotification != null)
                {
                    await _notificationService.SendAndSaveNotificationAsync(doctorNotification);
                }
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
                var invoice = await _dataContext.VcInvoices
                    .FirstOrDefaultAsync(x => x.AppointmentId == appointment.Id && x.IsActive, cancellationToken);
                if (invoice == null)
                {
                    throw new ArgumentException(_localizer["invoice.not_found"]);
                }

                if (invoice.TotalAmount <= 0)
                {
                    throw new ArgumentException(_localizer["invoice.amount.invalid"]);
                }

                if (invoice.Status == PaymentStatus.SUCCESS.ToString())
                {
                    throw new ArgumentException(_localizer["invoice.already_paid"]);
                }

                var payment = new VcPayments
                {
                    InvoiceId = invoice.Id,
                    Code = GenerateCodeUtils.GenerateUserCode("PAY"),
                    PaymentMethod = PaymentMethod.CASH.ToString(),
                    PaymentStatus = PaymentStatus.SUCCESS.ToString(),
                    Amount = invoice.TotalAmount,
                    GatewayTransactionId = null,
                    ResponseCode = null,
                    GatewayResponse = null,
                    PaymentDate = DateTime.UtcNow,
                    IsActive = true,
                    Order = 0,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUserId = _contextAccessor.UserId,
                    CreatedUserName = _contextAccessor.UserName
                };

                invoice.Status = PaymentStatus.SUCCESS.ToString();
                invoice.PaidDate = DateTime.UtcNow;

                await _dataContext.VcPayments.AddAsync(payment, cancellationToken);
            }
        }
    }
}
