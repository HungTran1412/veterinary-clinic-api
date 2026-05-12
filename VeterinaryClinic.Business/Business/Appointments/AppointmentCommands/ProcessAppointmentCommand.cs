using MediatR;
using Microsoft.Extensions.Localization;
using Serilog;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using VeterinaryClinic.Shared.ContextAccessor;

namespace VeterinaryClinic.Business
{
    public class ProcessAppointmentCommand : IRequest<Unit>
    {
        public int AppointmentId { get; }
        public ProcessAppointmentModel Model { get; }

        public ProcessAppointmentCommand(int appointmentId, ProcessAppointmentModel model)
        {
            AppointmentId = appointmentId;
            Model = model;
        }

        public class Handler : IRequestHandler<ProcessAppointmentCommand, Unit>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly IContextAccessor _contextAccessor;
            private readonly IStringLocalizer<ProcessAppointmentCommand> _localizer;
            private readonly ICacheService _cacheService;
            private readonly IAppointmentStateMachine _appointmentStateMachine;

            public Handler(
                VeterinaryClinicDataContext dataContext,
                Func<IContextAccessor> contextAccessorFactory,
                IStringLocalizer<ProcessAppointmentCommand> localizer,
                ICacheService cacheService,
                IAppointmentStateMachine appointmentStateMachine)
            {
                _dataContext = dataContext;
                _contextAccessor = contextAccessorFactory();
                _localizer = localizer;
                _cacheService = cacheService;
                _appointmentStateMachine = appointmentStateMachine;
            }

            public async Task<Unit> Handle(ProcessAppointmentCommand request, CancellationToken cancellationToken)
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
                _appointmentStateMachine.Apply(appointment, action, request.Model.CancelReason);

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
                    AppointmentAction.REQUEST_CANCELLATION => isCustomer && appointment.CustomerId == userId,
                    AppointmentAction.APPROVE_CANCELLATION => isAdmin || isReceptionist,
                    AppointmentAction.REJECT_CANCELLATION_REQUEST => isAdmin || isReceptionist,
                    AppointmentAction.MARK_NO_SHOW => isAdmin || (isDoctor && appointment.DoctorId == userId),
                    AppointmentAction.COMPLETE_CONSULTATION => isDoctor && appointment.DoctorId == userId,
                    AppointmentAction.COMPLETE_PAYMENT => isAdmin || isReceptionist,
                    AppointmentAction.CASH_PAYMENT => isAdmin || isReceptionist,
                    AppointmentAction.BANK_TRANSFER => isAdmin || isReceptionist,
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
