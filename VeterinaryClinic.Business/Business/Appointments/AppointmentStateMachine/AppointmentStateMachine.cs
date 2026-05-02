using Microsoft.Extensions.Localization;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public interface IAppointmentStateMachine
    {
        AppointmentStatus GetInitialStatus();
        string GetStateDisplayName(AppointmentStatus status);
        bool IsFinalStatus(AppointmentStatus status);
        IReadOnlyCollection<AppointmentAction> GetAvailableActions(AppointmentStatus status, Role role);
        void Apply(VcAppointments appointment, AppointmentAction action, string? cancelReason = null);
        string GetActionDisplayName(AppointmentAction action);
    }

    public class AppointmentStateMachine : IAppointmentStateMachine
    {
        private readonly IStringLocalizer<AppointmentStateMachine> _localizer;
        private readonly IContextAccessor _contextAccessor;

        private static readonly HashSet<AppointmentStatus> FinalStatuses = new()
        {
            AppointmentStatus.COMPLETED,
            AppointmentStatus.CANCELLED,
            AppointmentStatus.REJECTED,
            AppointmentStatus.NO_SHOW
        };

        private static readonly Dictionary<AppointmentStatus, IReadOnlyCollection<AppointmentAction>> AllowedActions =
            new()
            {
                [AppointmentStatus.CONFIRMED] = new[]
                {
                    AppointmentAction.START_CONSULTATION,
                    AppointmentAction.REQUEST_CANCELLATION,
                    AppointmentAction.MARK_NO_SHOW
                },
                [AppointmentStatus.CANCELLATION_REQUESTED] = new[]
                {
                    AppointmentAction.APPROVE_CANCELLATION,
                    AppointmentAction.REJECT_CANCELLATION_REQUEST
                },
                [AppointmentStatus.IN_PROGRESS] = new[]
                {
                    AppointmentAction.COMPLETE_CONSULTATION
                },
                [AppointmentStatus.PAYMENT_PENDING] = new[]
                {
                    AppointmentAction.COMPLETE_PAYMENT
                }
            };

        public AppointmentStateMachine(IStringLocalizer<AppointmentStateMachine> localizer)
        {
            _localizer = localizer;
        }

        public AppointmentStatus GetInitialStatus()
        {
            return AppointmentStatus.CONFIRMED;
        }

        public string GetStateDisplayName(AppointmentStatus status)
        {
            return status switch
            {
                AppointmentStatus.CONFIRMED => "Đã xác nhận",
                AppointmentStatus.REJECTED => "Từ chối",
                AppointmentStatus.CANCELLED => "Đã hủy",
                AppointmentStatus.CANCELLATION_REQUESTED => "Chờ duyệt hủy",
                AppointmentStatus.IN_PROGRESS => "Đang thực hiện",
                AppointmentStatus.PAYMENT_PENDING => "Chờ thanh toán",
                AppointmentStatus.COMPLETED => "Hoàn thành",
                AppointmentStatus.NO_SHOW => "Không đến",
                _ => status.ToString()
            };
        }

        public bool IsFinalStatus(AppointmentStatus status)
        {
            return FinalStatuses.Contains(status);
        }

        public IReadOnlyCollection<AppointmentAction> GetAvailableActions(AppointmentStatus status, Role role)
        {
            if (!AllowedActions.TryGetValue(status, out var actions))
                return Array.Empty<AppointmentAction>();

            return role switch
            {
                Role.CUSTOMER => actions.Where(x =>
                    x == AppointmentAction.REQUEST_CANCELLATION
                ).ToList(),

                Role.DOCTOR => actions.Where(x =>
                    x == AppointmentAction.START_CONSULTATION ||
                    x == AppointmentAction.COMPLETE_CONSULTATION ||
                    x == AppointmentAction.MARK_NO_SHOW
                ).ToList(),

                Role.RECEPTIONIST => actions.Where(x =>
                    x == AppointmentAction.APPROVE_CANCELLATION ||
                    x == AppointmentAction.REJECT_CANCELLATION_REQUEST
                ).ToList(),
                
                Role.ADMIN => new List<AppointmentAction>(),
                
                _ => new List<AppointmentAction>()
            };
        }

        public void Apply(VcAppointments appointment, AppointmentAction action, string? cancelReason = null)
        {
            var currentStatus = ParseStatus(appointment.State);
            var role = Enum.Parse<Role>(_contextAccessor.Role);

            if (action == AppointmentAction.REQUEST_CANCELLATION)
            {
                var now = DateTime.UtcNow;

                if (appointment.StartTime <= now)
                {
                    throw new InvalidOperationException("Lịch đã bắt đầu hoặc đã qua, không thể hủy.");
                }

                var timeDiff = appointment.StartTime - now;

                if (timeDiff.TotalHours < 1)
                {
                    throw new InvalidOperationException("Chỉ được hủy lịch trước 1 giờ.");
                }
            }

            var nextStatus = GetNextStatus(currentStatus, action, role);

            // 🔥 validate lý do hủy
            if ((nextStatus == AppointmentStatus.CANCELLED ||
                 nextStatus == AppointmentStatus.REJECTED) &&
                string.IsNullOrWhiteSpace(cancelReason))
            {
                throw new ArgumentException(_localizer["appointment.cancel_reason.required"]);
            }

            appointment.State = nextStatus.ToString();
            appointment.StateName = GetStateDisplayName(nextStatus);
            appointment.IsFinalState = IsFinalStatus(nextStatus);

            if (nextStatus is AppointmentStatus.CANCELLED or AppointmentStatus.REJECTED)
            {
                appointment.CancelReason = cancelReason!.Trim();
            }
        }

        private AppointmentStatus ParseStatus(string? state)
        {
            if (string.IsNullOrWhiteSpace(state) ||
                !Enum.TryParse<AppointmentStatus>(state, true, out var status))
            {
                throw new ArgumentException(_localizer["appointment.state.invalid"]);
            }

            return status;
        }

        private AppointmentStatus GetNextStatus(AppointmentStatus currentStatus, AppointmentAction action, Role role)
        {
            if (!GetAvailableActions(currentStatus, role).Contains(action))
            {
                throw new InvalidOperationException(_localizer["appointment.transition.invalid"]);
            }

            return (currentStatus, action) switch
            {
                // (AppointmentStatus.PENDING_CONFIRMATION, AppointmentAction.CONFIRM) => AppointmentStatus.CONFIRMED,
                // (AppointmentStatus.PENDING_CONFIRMATION, AppointmentAction.REJECT) => AppointmentStatus.REJECTED,
                // (AppointmentStatus.PENDING_CONFIRMATION, AppointmentAction.CUSTOMER_CANCEL) => AppointmentStatus.CANCELLED,
                (AppointmentStatus.CONFIRMED, AppointmentAction.START_CONSULTATION) => AppointmentStatus.IN_PROGRESS,
                (AppointmentStatus.CONFIRMED, AppointmentAction.REQUEST_CANCELLATION) => AppointmentStatus
                    .CANCELLATION_REQUESTED,
                (AppointmentStatus.CONFIRMED, AppointmentAction.MARK_NO_SHOW) => AppointmentStatus.NO_SHOW,
                (AppointmentStatus.CANCELLATION_REQUESTED, AppointmentAction.APPROVE_CANCELLATION) => AppointmentStatus
                    .CANCELLED,
                (AppointmentStatus.CANCELLATION_REQUESTED, AppointmentAction.REJECT_CANCELLATION_REQUEST) =>
                    AppointmentStatus.CONFIRMED,
                (AppointmentStatus.IN_PROGRESS, AppointmentAction.COMPLETE_CONSULTATION) => AppointmentStatus
                    .PAYMENT_PENDING,
                (AppointmentStatus.PAYMENT_PENDING, AppointmentAction.COMPLETE_PAYMENT) => AppointmentStatus.COMPLETED,
                _ => throw new InvalidOperationException(_localizer["appointment.transition.invalid"])
            };
        }

        public string GetActionDisplayName(AppointmentAction action)
        {
            return action switch
            {
                AppointmentAction.CONFIRM => "Xác nhận",
                AppointmentAction.REJECT => "Từ chối",
                AppointmentAction.CUSTOMER_CANCEL => "Hủy lịch",

                AppointmentAction.START_CONSULTATION => "Bắt đầu khám",
                AppointmentAction.REQUEST_CANCELLATION => "Yêu cầu hủy",
                AppointmentAction.MARK_NO_SHOW => "Không đến",

                AppointmentAction.APPROVE_CANCELLATION => "Xác nhận hủy",
                AppointmentAction.REJECT_CANCELLATION_REQUEST => "Từ chối hủy",

                AppointmentAction.COMPLETE_CONSULTATION => "Hoàn thành khám",
                AppointmentAction.COMPLETE_PAYMENT => "Thanh toán",

                _ => action.ToString()
            };
        }
    }
}