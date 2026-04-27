using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class GetWorkScheduleByIdQuery : IRequest<WorkScheduleModel>
    {
        public int Id { get; }

        public GetWorkScheduleByIdQuery(int id)
        {
            Id = id;
        }

        public class Handler : IRequestHandler<GetWorkScheduleByIdQuery, WorkScheduleModel>
        {
            private readonly VeterinaryClinicReadDataContext _dataContext;
            private readonly IStringLocalizer<DeleteWorkScheduleCommand> _localizer;

            public Handler(VeterinaryClinicReadDataContext dataContext, IStringLocalizer<DeleteWorkScheduleCommand> localizer)
            {
                _dataContext = dataContext;
                _localizer = localizer;
            }

            public async Task<WorkScheduleModel> Handle(GetWorkScheduleByIdQuery request, CancellationToken cancellationToken)
            {
                var query = from ws in _dataContext.VcWorkSchedules
                            join u in _dataContext.VcUsers on ws.UserId equals u.Id
                            where ws.Id == request.Id && ws.IsActive
                            select new WorkScheduleModel
                            {
                                Id = ws.Id,
                                Code = ws.Code,
                                UserId = ws.UserId,
                                WorkDate = ws.WorkDate,
                                StartTime = ws.StartTime,
                                EndTime = ws.EndTime,
                                ShiftName = ws.ShiftName,
                                Note = ws.Note,
                                IsActive = ws.IsActive,
                                Order = ws.Order,
                                CreatedDate = ws.CreatedDate,
                                FullName = u.FullName,
                                Role = u.Role
                            };

                var workSchedule = await query.AsNoTracking().FirstOrDefaultAsync(cancellationToken);

                if (workSchedule == null)
                {
                    throw new KeyNotFoundException(_localizer["work_schedule.not_found"]);
                }

                return workSchedule;
            }
        }
    }
}
