using MediatR;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VeterinaryClinic.Business
{
    public class GetFilterNotificationQuery : IRequest<PaginationList<NotificationModel>>
    {
        public NotificationFilterModel Filter { get; set; }

        public GetFilterNotificationQuery(NotificationFilterModel filter)
        {
            Filter = filter;
        }

        public class Handler : IRequestHandler<GetFilterNotificationQuery, PaginationList<NotificationModel>>
        {
            private readonly VeterinaryClinicReadDataContext _dataContext;
            private readonly IContextAccessor _contextAccessor;

            public Handler(VeterinaryClinicReadDataContext dataContext, Func<IContextAccessor> contextAccessorFactory)
            {
                _dataContext = dataContext;
                _contextAccessor = contextAccessorFactory();
            }

            public async Task<PaginationList<NotificationModel>> Handle(GetFilterNotificationQuery request, CancellationToken cancellationToken)
            {
                var filter = request.Filter;
                var currentUserId = _contextAccessor.UserId;

                if (!currentUserId.HasValue)
                {
                    return new PaginationList<NotificationModel>();
                }

                var query = from n in _dataContext.VcNotifications.AsNoTracking()
                            join u in _dataContext.VcUsers.AsNoTracking() on n.UserId equals u.Id
                            where n.UserId == currentUserId.Value
                            select new NotificationModel
                            {
                                Id = n.Id,
                                UserId = n.UserId,
                                UserFullName = u.FullName,
                                Title = n.Title,
                                Message = n.Title,
                                Type = n.Type,
                                IsRead = n.IsRead,
                                RelatedEntityId = n.RelatedEntityId,
                                RelatedEntityType = ((RelatedEntityType)n.RelatedEntityType).ToString(),
                                CreatedDate = n.CreatedDate
                            };

                if (!string.IsNullOrEmpty(filter.TextSearch))
                {
                    string ts = filter.TextSearch.Trim().ToLower();
                    query = query.Where(x => x.Title.ToLower().Contains(ts));
                }

                if (!string.IsNullOrEmpty(filter.Type))
                {
                    query = query.Where(n => n.Type == filter.Type);
                }

                // if (filter.IsRead.HasValue)
                // {
                //     query = query.Where(x => x.IsRead == filter.IsRead.Value);
                // }

                if (string.IsNullOrEmpty(filter.PropertyName))
                {
                    query = query.OrderByDescending(x => x.CreatedDate);
                }
                else
                {
                    query = query.OrderByField(filter.PropertyName, filter.Ascending);
                }

                query = query.OrderByDescending(x => x.CreatedDate).ThenByDescending(x=>x.Id);
                
                if (filter.PageSize <= 0)
                {
                    filter.PageSize = 10;
                }
                if (filter.PageNumber <= 0)
                {
                    filter.PageNumber = 1;
                }

                int totalCount = await query.CountAsync(cancellationToken);

                int excludedRows = (filter.PageNumber - 1) * filter.PageSize;
                if (excludedRows < 0) excludedRows = 0;

                var listData = await query
                    .Skip(excludedRows)
                    .Take(filter.PageSize)
                    .ToListAsync(cancellationToken);

                return new PaginationList<NotificationModel>()
                {
                    DataCount = listData.Count,
                    TotalCount = totalCount,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize,
                    Data = listData
                };
            }
        }
    }
}
