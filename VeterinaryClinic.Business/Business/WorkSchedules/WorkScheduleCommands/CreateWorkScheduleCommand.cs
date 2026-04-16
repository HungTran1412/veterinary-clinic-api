using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using System.Text.Json;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using VeterinaryClinic.Shared.ContextAccessor;

namespace VeterinaryClinic.Business
{
    public class CreateWorkScheduleCommand : IRequest<int>
    {
        public CreateWorkScheduleModel Model { get; }

        public CreateWorkScheduleCommand(CreateWorkScheduleModel model)
        {
            Model = model;
        }

        public class Handler : IRequestHandler<CreateWorkScheduleCommand, int>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly ICacheService _cacheService;
            private readonly IContextAccessor _contextAccessor;
            private readonly IStringLocalizer<CreateWorkScheduleCommand> _localizer;

            public Handler(VeterinaryClinicDataContext dataContext, ICacheService cacheService, Func<IContextAccessor> contextAccessorFactory, IStringLocalizer<CreateWorkScheduleCommand> localizer)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
                _contextAccessor = contextAccessorFactory();
                _localizer = localizer;
            }

            public async Task<int> Handle(CreateWorkScheduleCommand request, CancellationToken cancellationToken)
            {
                var model = request.Model;
                Log.Information($"Create WorkSchedule: {JsonSerializer.Serialize(model)}");

                // 0. Validate WorkDate is not in the past
                if (model.WorkDate.Date < DateTime.Now.Date)
                {
                    throw new ArgumentException(_localizer["work_schedule.work_date.cannot_be_in_the_past"]);
                }

                // 1. Validate EndTime > StartTime
                if (model.EndTime <= model.StartTime)
                {
                    throw new ArgumentException(_localizer["work_schedule.end_time.must_be_after_start_time"]);
                }

                // 2. Validate User exists and has correct role
                var user = await _dataContext.VcUsers.FindAsync(model.UserId);
                if (user == null)
                {
                    throw new KeyNotFoundException(_localizer["work_schedule.user.not_found"]);
                }

                var validRoles = new[] { Role.DOCTOR.ToString(), Role.RECEPTIONIST.ToString() };
                if (!validRoles.Contains(user.Role))
                {
                    throw new InvalidOperationException(_localizer["work_schedule.user.invalid_role"]);
                }

                // 3. Validate for schedule conflicts
                var conflictExists = await _dataContext.VcWorkSchedules
                    .AnyAsync(ws =>
                        ws.IsActive &&
                        ws.UserId == model.UserId &&
                        ws.WorkDate.Date == model.WorkDate.Date &&
                        model.StartTime < ws.EndTime &&
                        model.EndTime > ws.StartTime,
                        cancellationToken);

                if (conflictExists)
                {
                    throw new InvalidOperationException(_localizer["work_schedule.schedule.conflict"]);
                }

                // Manual mapping to ensure all required fields are set
                var entity = new VcWorkSchedules
                {
                    Code = GenerateCodeUtils.GenerateUserCode("WS"),
                    UserId = model.UserId,
                    WorkDate = model.WorkDate,
                    StartTime = model.StartTime,
                    EndTime = model.EndTime,
                    ShiftName = model.ShiftName,
                    Note = model.Note ?? string.Empty, 
                    IsActive = true,
                    Order = 0,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUserId = _contextAccessor.UserId,
                    CreatedUserName = _contextAccessor.UserName
                };

                await _dataContext.VcWorkSchedules.AddAsync(entity, cancellationToken);
                await _dataContext.SaveChangesAsync(cancellationToken);

                // Remove cache
                _cacheService.Remove(WorkScheduleConstant.BuildCacheKey(string.Empty));

                Log.Information($"WorkSchedule created successfully with Id: {entity.Id}");

                return entity.Id;
            }
        }
    }
}
