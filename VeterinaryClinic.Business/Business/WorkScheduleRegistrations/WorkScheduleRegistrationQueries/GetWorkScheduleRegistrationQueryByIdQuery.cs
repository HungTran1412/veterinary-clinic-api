using MediatR;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using VeterinaryClinic.Shared.ContextAccessor;

namespace VeterinaryClinic.Business;

public class GetWorkScheduleRegistrationQueryByIdQuery : IRequest<WorkScheduleRegistrationModel>
{
    public int Id { get; }

    public GetWorkScheduleRegistrationQueryByIdQuery(int id)
    {
        Id = id;
    }

    public class Handler : IRequestHandler<GetWorkScheduleRegistrationQueryByIdQuery, WorkScheduleRegistrationModel>
    {
        private readonly VeterinaryClinicReadDataContext _dataContext;
        private readonly IContextAccessor _contextAccessor;

        public Handler(VeterinaryClinicReadDataContext dataContext, Func<IContextAccessor> contextAccessorFactory)
        {
            _dataContext = dataContext;
            _contextAccessor = contextAccessorFactory();
        }

        public async Task<WorkScheduleRegistrationModel> Handle(GetWorkScheduleRegistrationQueryByIdQuery request, CancellationToken cancellationToken)
        {
            var item = await (
                from reg in _dataContext.VcWorkScheduleRegistrations.AsNoTracking()
                join user in _dataContext.VcUsers.AsNoTracking() on reg.UserId equals user.Id
                join shift in _dataContext.VcShiftTemplates.AsNoTracking() on reg.ShiftTemplateId equals shift.Id
                where reg.IsActive && reg.Id == request.Id
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
                }).FirstOrDefaultAsync(cancellationToken);

            if (item == null)
            {
                throw new ArgumentException("Work schedule registration not found.");
            }

            if (_contextAccessor.Role != Role.ADMIN.ToString() && item.UserId != _contextAccessor.UserId)
            {
                throw new UnauthorizedAccessException("Unauthorized.");
            }

            return item;
        }
    }
}
