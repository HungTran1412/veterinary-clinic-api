using Microsoft.Extensions.Localization;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using VeterinaryClinic.Shared.ContextAccessor;

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

        private static readonly Dictionary<AppointmentStatus, IReadOnlyCollection<AppointmentAction>> AllowedActions = new()
        {
            [AppointmentStatus.CONFIRMED] = new[]
            {
                AppointmentAction.START_CONSULTATION,
                AppointmentAction.CUSTOMER_CANCEL,
                AppointmentAction.MARK_NO_SHOW
            },
            [AppointmentStatus.IN_PROGRESS] = new[]
            {
                AppointmentAction.COMPLETE_CONSULTATION
            },
            [AppointmentStatus.PAYMENT_PENDING] = new[]
            {
                AppointmentAction.CASH_PAYMENT,
                AppointmentAction.BANK_TRANSFER
            }
        };

        public AppointmentStateMachine(
            IStringLocalizer<AppointmentStateMachine> localizer,
            Func<IContextAccessor> contextAccessorFactory)
        {
            _localizer = localizer;
            _contextAccessor = contextAccessorFactory();
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
            {
                return Array.Empty<AppointmentAction>();
            }

            return role switch
            {
                Role.CUSTOMER => actions.Where(x =>
                    x == AppointmentAction.CUSTOMER_CANCEL ||
                    x == AppointmentAction.BANK_TRANSFER
                ).ToList(),

                Role.DOCTOR => actions.Where(x =>
                    x == AppointmentAction.START_CONSULTATION ||
                    x == AppointmentAction.COMPLETE_CONSULTATION ||
                    x == AppointmentAction.MARK_NO_SHOW
                ).ToList(),

                Role.RECEPTIONIST => actions.Where(x =>
                    x == AppointmentAction.CASH_PAYMENT ||
                    x == AppointmentAction.BANK_TRANSFER
                ).ToList(),

                Role.ADMIN => actions.ToList(),

                _ => new List<AppointmentAction>()
            };
        }

        public void Apply(VcAppointments appointment, AppointmentAction action, string? cancelReason = null)
        {
            var currentStatus = ParseStatus(appointment.State);
            if (string.IsNullOrWhiteSpace(_contextAccessor.Role) ||
                !Enum.TryParse<Role>(_contextAccessor.Role, true, out var role))
            {
                throw new UnauthorizedAccessException(_localizer["user.unauthorized"]);
            }

            if (action == AppointmentAction.CUSTOMER_CANCEL)
            {
                var now = DateTime.UtcNow;

                if (appointment.StartTime <= now)
                {
                    throw new InvalidOperationException(_localizer["appointment.cancel.started_or_past"]);
                }

                var timeDiff = appointment.StartTime - now;

                if (timeDiff.TotalHours < 1)
                {
                    throw new InvalidOperationException(_localizer["appointment.cancel.less_than_one_hour"]);
                }

                if (!string.IsNullOrWhiteSpace(cancelReason))
                {
                    appointment.CancelReason = cancelReason.Trim();
                }
            }

            var nextStatus = GetNextStatus(currentStatus, action, role);

            if (nextStatus == AppointmentStatus.CANCELLED &&
                string.IsNullOrWhiteSpace(cancelReason) &&
                string.IsNullOrWhiteSpace(appointment.CancelReason))
            {
                throw new ArgumentException(_localizer["appointment.cancel_reason.required"]);
            }

            appointment.State = nextStatus.ToString();
            appointment.StateName = GetStateDisplayName(nextStatus);
            appointment.IsFinalState = IsFinalStatus(nextStatus);

            if (nextStatus == AppointmentStatus.CANCELLED)
            {
                appointment.CancelReason = string.IsNullOrWhiteSpace(cancelReason)
                    ? appointment.CancelReason
                    : cancelReason.Trim();
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
                (AppointmentStatus.CONFIRMED, AppointmentAction.START_CONSULTATION) => AppointmentStatus.IN_PROGRESS,
                (AppointmentStatus.CONFIRMED, AppointmentAction.CUSTOMER_CANCEL) => AppointmentStatus.CANCELLED,
                (AppointmentStatus.CONFIRMED, AppointmentAction.MARK_NO_SHOW) => AppointmentStatus.NO_SHOW,
                (AppointmentStatus.IN_PROGRESS, AppointmentAction.COMPLETE_CONSULTATION) => AppointmentStatus.PAYMENT_PENDING,
                (AppointmentStatus.PAYMENT_PENDING, AppointmentAction.COMPLETE_PAYMENT) => AppointmentStatus.COMPLETED,
                (AppointmentStatus.PAYMENT_PENDING, AppointmentAction.CASH_PAYMENT) => AppointmentStatus.COMPLETED,
                (AppointmentStatus.PAYMENT_PENDING, AppointmentAction.BANK_TRANSFER) => AppointmentStatus.COMPLETED,
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
                AppointmentAction.MARK_NO_SHOW => "Không đến",
                AppointmentAction.COMPLETE_CONSULTATION => "Hoàn thành khám",
                AppointmentAction.COMPLETE_PAYMENT => "Thanh toán",
                AppointmentAction.CASH_PAYMENT => "Thanh toán tiền mặt",
                AppointmentAction.BANK_TRANSFER => "Chuyển khoản",
                _ => action.ToString()
            };
        }
    }
}
