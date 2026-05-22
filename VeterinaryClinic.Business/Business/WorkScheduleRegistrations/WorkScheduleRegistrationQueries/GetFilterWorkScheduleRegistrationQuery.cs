using MediatR;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using VeterinaryClinic.Shared.ContextAccessor;

namespace VeterinaryClinic.Business;

public class GetFilterWorkScheduleRegistrationQuery : IRequest<PaginationList<WorkScheduleRegistrationModel>>
{
    public WorkScheduleRegistrationFilterModel Filter { get; }

    public GetFilterWorkScheduleRegistrationQuery(WorkScheduleRegistrationFilterModel filter)
    {
        Filter = filter;
    }

    public class Handler : IRequestHandler<GetFilterWorkScheduleRegistrationQuery, PaginationList<WorkScheduleRegistrationModel>>
    {
        private readonly VeterinaryClinicReadDataContext _dataContext;
        private readonly IContextAccessor _contextAccessor;

        public Handler(VeterinaryClinicReadDataContext dataContext, Func<IContextAccessor> contextAccessorFactory)
        {
            _dataContext = dataContext;
            _contextAccessor = contextAccessorFactory();
        }

        public async Task<PaginationList<WorkScheduleRegistrationModel>> Handle(GetFilterWorkScheduleRegistrationQuery request, CancellationToken cancellationToken)
        {
            var filter = request.Filter;

            var query =
                from reg in _dataContext.VcWorkScheduleRegistrations.AsNoTracking()
                join user in _dataContext.VcUsers.AsNoTracking() on reg.UserId equals user.Id
                join shift in _dataContext.VcShiftTemplates.AsNoTracking() on reg.ShiftTemplateId equals shift.Id
                where reg.IsActive
                select new WorkScheduleRegistrationModel
                {
                    Id = reg.Id,
                    Code = reg.Code,
                    UserId = reg.UserId,
                    UserCode = user.Code,
                    FullName = user.FullName,
                    Role = user.Role,
                    ShiftTemplateId = reg.ShiftTemplateId,
                    ShiftName = shift.ShiftName,
                    ShiftStartTime = shift.StartTime,
                    ShiftEndTime = shift.EndTime,
                    WorkDate = reg.WorkDate,
                    Status = reg.Status,
                    RegisterDate = reg.RegisteredDate,
                    Note = reg.Note,
                    IsActive = reg.IsActive,
                    Order = reg.Order,
                    CreatedDate = reg.CreatedDate
                };

            if (!string.IsNullOrWhiteSpace(filter.TextSearch))
            {
                var ts = filter.TextSearch.Trim().ToLower();
                query = query.Where(x =>
                    x.Code.ToLower().Contains(ts) ||
                    x.UserCode.ToLower().Contains(ts) ||
                    x.FullName.ToLower().Contains(ts) ||
                    x.ShiftName.ToLower().Contains(ts));
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                var status = filter.Status.Trim().ToUpper();
                query = query.Where(x => x.Status.ToUpper() == status);
            }

            if (filter.ShiftTemplateId.HasValue)
            {
                query = query.Where(x => x.ShiftTemplateId == filter.ShiftTemplateId.Value);
            }

            if (filter.FromWorkDate.HasValue)
            {
                query = query.Where(x => x.WorkDate.Date >= filter.FromWorkDate.Value.Date);
            }

            if (filter.ToWorkDate.HasValue)
            {
                query = query.Where(x => x.WorkDate.Date <= filter.ToWorkDate.Value.Date);
            }

            if (_contextAccessor.Role != Role.ADMIN.ToString())
            {
                if (!_contextAccessor.UserId.HasValue)
                {
                    return new PaginationList<WorkScheduleRegistrationModel>();
                }

                query = query.Where(x => x.UserId == _contextAccessor.UserId.Value);
            }

            query = query.OrderByField(filter.PropertyName, filter.Ascending);

            if (filter.PageSize <= 0)
            {
                filter.PageSize = 10;
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var excludedRows = (filter.PageNumber - 1) * filter.PageSize;
            if (excludedRows < 0)
            {
                excludedRows = 0;
            }

            var listData = await query
                .Skip(excludedRows)
                .Take(filter.PageSize)
                .ToListAsync(cancellationToken);

            return new PaginationList<WorkScheduleRegistrationModel>
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
