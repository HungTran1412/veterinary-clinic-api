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
                var workSchedule = await _dataContext.VcWorkSchedules
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ws => ws.Id == request.Id && ws.IsActive, cancellationToken);

                if (workSchedule == null)
                {
                    throw new KeyNotFoundException(_localizer["work_schedule.user.not_found"]);
                }

                return AutoMapperUtils.AutoMap<VcWorkSchedules, WorkScheduleModel>(workSchedule);
            }
        }
    }
}
