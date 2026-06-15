using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using System.Linq;
using System.Threading.Tasks;
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

                #region PAYMENT LOGIC

                if (action == AppointmentAction.CASH_PAYMENT || action == AppointmentAction.BANK_TRANSFER)
                {
                    // check xem hóa đơn được xử lý chưa
                    var primaryInvoice = await _dataContext.VcInvoices
                        .AsNoTracking()
                        .FirstOrDefaultAsync(i => i.AppointmentId == appointment.Id, cancellationToken);

                    if (primaryInvoice != null)
                    {
                        if (primaryInvoice.Status == PaymentStatus.PAID.ToString() ||
                            primaryInvoice.Status == PaymentStatus.SUCCESS.ToString())
                        {
                            throw new ArgumentException(_localizer["invoice.already_processed"]);
                        }

                        if (primaryInvoice.BillId.HasValue)
                        {
                            var existingBillStatus = await _dataContext.VcBills
                                .AsNoTracking()
                                .Where(b => b.Id == primaryInvoice.BillId.Value && b.IsActive)
                                .Select(b => b.Status)
                                .FirstOrDefaultAsync(cancellationToken);

                            if (string.Equals(existingBillStatus, PaymentStatus.PAID.ToString(), StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(existingBillStatus, PaymentStatus.SUCCESS.ToString(), StringComparison.OrdinalIgnoreCase))
                            {
                                throw new ArgumentException(_localizer["invoice.already_processed"]);
                            }
                        }
                    }
                    
                    // Xử lí các lịch hẹn vẫn còn hóa đơn chưa thanh toán
                    var pendingAppointmentIds = await (
                            from appt in _dataContext.VcAppointments
                            join invoice in _dataContext.VcInvoices on appt.Id equals invoice.AppointmentId
                            where appt.CustomerId == appointment.CustomerId &&
                                  appt.State == AppointmentStatus.PAYMENT_PENDING.ToString() &&
                                  appt.IsActive &&
                                  invoice.IsActive &&
                                  invoice.Status != PaymentStatus.PAID.ToString() &&
                                  invoice.Status != PaymentStatus.SUCCESS.ToString()
                            select appt.Id)
                        .Distinct()
                        .ToListAsync(cancellationToken);
                    
                    if (!pendingAppointmentIds.Contains(appointment.Id))
                    {
                        pendingAppointmentIds.Add(appointment.Id);
                    }

                    var createBillModel = new CreateBillModel
                    {
                        AppointmentIds = pendingAppointmentIds,
                        PaymentMethod = action == AppointmentAction.CASH_PAYMENT ? PaymentMethod.CASH.ToString() : PaymentMethod.VNPAY.ToString(),
                        Note = "Thanh toán gộp tự động",
                        ClientIpAddress = null 
                    };

                    return await _mediator.Send(new CreateBillCommand(createBillModel), cancellationToken);
                }

                #endregion

                // This part will only be executed for non-payment actions
                _appointmentStateMachine.Apply(appointment, action, request.Model.CancelReason);
                appointment.StateName = _appointmentStateMachine.GetStateDisplayName(Enum.Parse<AppointmentStatus>(appointment.State));

                if (action == AppointmentAction.COMPLETE_CONSULTATION)
                {
                    appointment.EndTime = DateTime.UtcNow;
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
        }
    }
}
