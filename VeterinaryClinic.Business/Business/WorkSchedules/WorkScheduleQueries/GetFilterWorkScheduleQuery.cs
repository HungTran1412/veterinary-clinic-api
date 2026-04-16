using MediatR;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{

    public class GetFilterWorkScheduleQuery : IRequest<List<WorkScheduleModel>>
    {
        public WorkScheduleFilterModel Filter { get; set; }

        public GetFilterWorkScheduleQuery(WorkScheduleFilterModel filter)
        {
            Filter = filter;
        }

        public class Handler : IRequestHandler<GetFilterWorkScheduleQuery, List<WorkScheduleModel>>
        {
            private readonly VeterinaryClinicReadDataContext _dataContext;
            private readonly IContextAccessor _contextAccessor;

            public Handler(VeterinaryClinicReadDataContext dataContext, Func<IContextAccessor> contextAccessorFactory)
            {
                _dataContext = dataContext;
                _contextAccessor = contextAccessorFactory();
            }

            public async Task<List<WorkScheduleModel>> Handle(GetFilterWorkScheduleQuery request, CancellationToken cancellationToken)
            {
                var filter = request.Filter;

                var query = from ws in _dataContext.VcWorkSchedules
                            join u in _dataContext.VcUsers on ws.UserId equals u.Id
                            where ws.IsActive
                            select new { WorkSchedule = ws, User = u };


                // Apply date filters
                if (filter.FromDate.HasValue)
                {
                    query = query.Where(x => x.WorkSchedule.WorkDate >= filter.FromDate.Value.Date);
                }

                if (filter.ToDate.HasValue)
                {
                    query = query.Where(x => x.WorkSchedule.WorkDate <= filter.ToDate.Value.Date);
                }

                // Filter by TextSearch (User Code and FullName)
                if (!string.IsNullOrEmpty(filter.TextSearch))
                {
                    string ts = filter.TextSearch.Trim().ToLower();
                    query = query.Where(x => 
                        x.User.Code.ToLower().Contains(ts) || 
                        x.User.FullName.ToLower().Contains(ts));
                }

                // Filter by Role
                if (!string.IsNullOrEmpty(filter.Role))
                {
                    string roleInput = filter.Role.Trim().ToUpper();
                    
                    if (Enum.TryParse<Role>(roleInput, true, out var parsedRole) && Enum.IsDefined(typeof(Role), parsedRole))
                    {
                        query = query.Where(x => x.User.Role.ToUpper() == roleInput);
                    }
                    else
                    {
                        return new List<WorkScheduleModel>();
                    }
                }

                // Security check: Role-based data access
                var currentUserId = _contextAccessor.UserId;
                var userRole = _contextAccessor.Role;

                // If the user is not an Admin, filter by their UserId
                if (userRole != Role.ADMIN.ToString())
                {
                    if (currentUserId.HasValue)
                    {
                        query = query.Where(x => x.WorkSchedule.UserId == currentUserId.Value);
                    }
                    else
                    {
                        // If a non-admin user has no UserId in the context, return an empty list
                        return new List<WorkScheduleModel>();
                    }
                }

                var listData = await query
                    .Select(x => new WorkScheduleModel
                    {
                        Id = x.WorkSchedule.Id,
                        Code = x.User.Code,
                        UserId = x.WorkSchedule.UserId,
                        FullName = x.User.FullName,
                        WorkDate = x.WorkSchedule.WorkDate,
                        StartTime = x.WorkSchedule.StartTime,
                        EndTime = x.WorkSchedule.EndTime,
                        ShiftName = x.WorkSchedule.ShiftName,
                        Note = x.WorkSchedule.Note,
                        IsActive = x.WorkSchedule.IsActive,
                        Order = x.WorkSchedule.Order,
                        CreatedDate = x.WorkSchedule.CreatedDate
                    })
                    .ToListAsync(cancellationToken);

                return listData;
            }
        }
    }
}
