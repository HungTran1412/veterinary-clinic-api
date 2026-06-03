using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class GetFilterTopServiceQuery : IRequest<PaginationList<TopServiceModel>>
    {
        public BaseQueryFilterModel Filter { get; }

        public GetFilterTopServiceQuery(BaseQueryFilterModel filter)
        {
            Filter = filter;
        }

        public class Handler : IRequestHandler<GetFilterTopServiceQuery, PaginationList<TopServiceModel>>
        {
            private readonly VeterinaryClinicReadDataContext _dataContext;

            public Handler(VeterinaryClinicReadDataContext dataContext)
            {
                _dataContext = dataContext;
            }

            public async Task<PaginationList<TopServiceModel>> Handle(GetFilterTopServiceQuery request, CancellationToken cancellationToken)
            {
                var filter = request.Filter;

                // 1. Group appointments by ServiceId and count them
                var serviceUsage = _dataContext.VcAppointments
                    .AsNoTracking()
                    .Where(a => a.IsActive) // Consider only active appointments
                    .GroupBy(a => a.ServiceId)
                    .Select(g => new
                    {
                        ServiceId = g.Key,
                        UsageCount = g.Count()
                    });

                // 2. Join with services table to get service details and order by usage count
                var data = from usage in serviceUsage
                           join s in _dataContext.VcServices.AsNoTracking() on usage.ServiceId equals s.Id
                           join sp in _dataContext.VcSpecializations.AsNoTracking() on s.SpecializationId equals sp.Id into spGroup
                           from sp in spGroup.DefaultIfEmpty()
                           where s.IsActive
                           select new TopServiceModel
                           {
                               Id = s.Id,
                               Code = s.Code,
                               Name = s.Name,
                               Price = s.Price,
                               DurationMinutes = s.DurationMinutes,
                               ImageUrl = s.ImageUrl,
                               SpecializationId = s.SpecializationId,
                               SpecializationName = sp.Name,
                               IsAvailable = s.IsAvailable,
                               IsActive = s.IsActive,
                               UsageCount = usage.UsageCount
                           };
                
                // Order by the most used services first
                data = data.OrderByDescending(x => x.UsageCount).ThenByDescending(x => x.Id);

                // 3. Apply pagination
                if (filter.PageSize <= 0)
                {
                    filter.PageSize = 10;
                }

                int totalCount = await data.CountAsync(cancellationToken);

                int excludedRows = (filter.PageNumber - 1) * filter.PageSize;
                if (excludedRows < 0) excludedRows = 0;

                var listData = await data
                    .Skip(excludedRows)
                    .Take(filter.PageSize)
                    .ToListAsync(cancellationToken);

                return new PaginationList<TopServiceModel>()
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
