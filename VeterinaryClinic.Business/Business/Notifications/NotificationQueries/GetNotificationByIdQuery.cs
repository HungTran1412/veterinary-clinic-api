using MediatR;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VeterinaryClinic.Business
{
    public class GetNotificationByIdQuery : IRequest<NotificationModel>
    {
        public int Id { get; }

        public GetNotificationByIdQuery(int id)
        {
            Id = id;
        }

        public class Handler : IRequestHandler<GetNotificationByIdQuery, NotificationModel>
        {
            private readonly VeterinaryClinicReadDataContext _dataContext;
            private readonly IContextAccessor _contextAccessor;

            public Handler(VeterinaryClinicReadDataContext dataContext, Func<IContextAccessor> contextAccessorFactory)
            {
                _dataContext = dataContext;
                _contextAccessor = contextAccessorFactory();
            }

            public async Task<NotificationModel> Handle(GetNotificationByIdQuery request, CancellationToken cancellationToken)
            {
                var currentUserId = _contextAccessor.UserId;

                if (!currentUserId.HasValue)
                {
                    return null;
                }

                var query = from notification in _dataContext.VcNotifications.AsNoTracking()
                            where notification.Id == request.Id && notification.UserId == currentUserId.Value
                            select new NotificationModel
                            {
                                Id = notification.Id,
                                UserId = notification.UserId,
                                Title = notification.Title,
                                Message = notification.Title, // Or a different field if you have one for full message content
                                Type = notification.Type,
                                RelatedEntityId = notification.RelatedEntityId,
                                RelatedEntityType = ((RelatedEntityType)notification.RelatedEntityType).ToString()
                            };

                var result = await query.FirstOrDefaultAsync(cancellationToken);

                if (result != null)
                {
                    // Optionally, you can add logic here to mark the notification as read
                    // This would require a write operation, so it might be better in a separate command
                }

                return result;
            }
        }
    }
}
