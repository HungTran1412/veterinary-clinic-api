using MediatR;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VeterinaryClinic.Business
{
    public class GetAllNotificationQuery : IRequest<List<NotificationModel>>
    {
        public GetAllNotificationQuery()
        {
        }

        public class Handler : IRequestHandler<GetAllNotificationQuery, List<NotificationModel>>
        {
            private readonly VeterinaryClinicReadDataContext _dataContext;
            private readonly IContextAccessor _contextAccessor;

            public Handler(VeterinaryClinicReadDataContext dataContext, Func<IContextAccessor> contextAccessorFactory)
            {
                _dataContext = dataContext;
                _contextAccessor = contextAccessorFactory();
            }

            public async Task<List<NotificationModel>> Handle(GetAllNotificationQuery request, CancellationToken cancellationToken)
            {
                var currentUserId = _contextAccessor.UserId;

                if (!currentUserId.HasValue)
                {
                    return new List<NotificationModel>();
                }

                var notifications = await _dataContext.VcNotifications
                    .AsNoTracking()
                    .Where(n => n.UserId == currentUserId.Value)
                    .OrderByDescending(n => n.CreatedDate)
                    .Select(n => new NotificationModel
                    {
                        Id = n.Id,
                        UserId = n.UserId,
                        Title = n.Title,
                        Message = n.Title,
                        Type = n.Type,
                        IsRead = n.IsRead,
                        RelatedEntityId = n.RelatedEntityId,
                        RelatedEntityType = ((RelatedEntityType)n.RelatedEntityType).ToString(),
                        CreatedDate = n.CreatedDate
                    })
                    .ToListAsync(cancellationToken);

                return notifications;
            }
        }
    }
}
