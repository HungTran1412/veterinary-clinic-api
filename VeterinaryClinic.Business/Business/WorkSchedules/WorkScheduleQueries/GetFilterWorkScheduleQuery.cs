using MediatR;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{

    public class GetFilterWorkScheduleQuery : IRequest<PaginationList<WorkScheduleModel>>
    {
        public WorkScheduleFilterModel Filter { get; set; }

        public GetFilterWorkScheduleQuery(WorkScheduleFilterModel filter)
        {
            Filter = filter;
        }

        public class Handler : IRequestHandler<GetFilterWorkScheduleQuery, PaginationList<WorkScheduleModel>>
        {
            private readonly VeterinaryClinicReadDataContext _dataContext;

            public Handler(VeterinaryClinicReadDataContext dataContext)
            {
                _dataContext = dataContext;
            }

            public async Task<PaginationList<WorkScheduleModel>> Handle(GetFilterWorkScheduleQuery request, CancellationToken cancellationToken)
            {
                var filter = request.Filter;

                var query = from ws in _dataContext.VcWorkSchedules
                            join u in _dataContext.VcUsers on ws.UserId equals u.Id
                            where ws.IsActive
                            select new { WorkSchedule = ws, User = u };

                if (filter.UserId.HasValue)
                {
                    query = query.Where(x => x.WorkSchedule.UserId == filter.UserId.Value);
                }

                if (!string.IsNullOrEmpty(filter.Role))
                {
                    query = query.Where(x => x.User.Role == filter.Role);
                }

                if (filter.FromDate.HasValue)
                {
                    query = query.Where(x => x.WorkSchedule.WorkDate >= filter.FromDate.Value.Date);
                }

                if (filter.ToDate.HasValue)
                {
                    query = query.Where(x => x.WorkSchedule.WorkDate <= filter.ToDate.Value.Date);
                }
                
                if (!string.IsNullOrEmpty(filter.TextSearch))
                {
                    string ts = filter.TextSearch.Trim().ToLower();
                    query = query.Where(x => x.User.FullName.ToLower().Contains(ts) || x.WorkSchedule.ShiftName.ToLower().Contains(ts));
                }

                var data = query.Select(x => x.WorkSchedule).OrderByField(filter.PropertyName, filter.Ascending);

                int totalCount = await data.CountAsync(cancellationToken);

                int excludedRows = (filter.PageNumber - 1) * filter.PageSize;
                if (excludedRows <= 0) excludedRows = 0;

                var listData = await data
                    .Skip(excludedRows)
                    .Take(filter.PageSize)
                    .ToListAsync(cancellationToken);

                var listResult = AutoMapperUtils.AutoMap<VcWorkSchedules, WorkScheduleModel>(listData);

                return new PaginationList<WorkScheduleModel>()
                {
                    DataCount = listResult.Count,
                    TotalCount = totalCount,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize,
                    Data = listResult
                };
            }
        }
    }
}
