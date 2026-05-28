using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using System.Threading;
using System.Threading.Tasks;

namespace VeterinaryClinic.Business
{
    public class ProcessNotifcationCommand : IRequest<Unit>
    {
        public int Id { get; }

        public ProcessNotifcationCommand(int id)
        {
            Id = id;
        }

        public class Handler : IRequestHandler<ProcessNotifcationCommand, Unit>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly IContextAccessor _contextAccessor;
            private readonly IStringLocalizer<ProcessNotifcationCommand> _localizer;

            public Handler(VeterinaryClinicDataContext dataContext, Func<IContextAccessor> contextAccessorFactory, IStringLocalizer<ProcessNotifcationCommand> localizer)
            {
                _dataContext = dataContext;
                _contextAccessor = contextAccessorFactory();
                _localizer = localizer;
            }

            public async Task<Unit> Handle(ProcessNotifcationCommand request, CancellationToken cancellationToken)
            {
                var currentUserId = _contextAccessor.UserId;
                if (!currentUserId.HasValue)
                {
                    throw new UnauthorizedAccessException(_localizer["user.unauthorized"]);
                }

                var notification = await _dataContext.VcNotifications
                    .FirstOrDefaultAsync(n => n.Id == request.Id && n.UserId == currentUserId.Value, cancellationToken);

                if (notification == null)
                {
                    // Using a generic not-found message is safer to not reveal information.
                    throw new KeyNotFoundException(_localizer["data.not-found"]);
                }

                if (!notification.IsRead)
                {
                    notification.IsRead = true;
                    notification.ModifiedDate = System.DateTime.UtcNow;
                    notification.ModifiedUserId = currentUserId;
                    await _dataContext.SaveChangesAsync(cancellationToken);
                }

                return Unit.Value;
            }
        }
    }
}
