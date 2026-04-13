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
    public class UpdateWorkScheduleCommand : IRequest<Unit>
    {
        public UpdateWorkScheduleModel Model { get; }
        public int Id { get; }

        public UpdateWorkScheduleCommand(int id, UpdateWorkScheduleModel model)
        {
            Id = id;
            Model = model;
        }

        public class Handler : IRequestHandler<UpdateWorkScheduleCommand, Unit>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly ICacheService _cacheService;
            private readonly IContextAccessor _contextAccessor;
            private readonly IStringLocalizer<UpdateWorkScheduleCommand> _localizer;

            public Handler(VeterinaryClinicDataContext dataContext, ICacheService cacheService, Func<IContextAccessor> contextAccessorFactory, IStringLocalizer<UpdateWorkScheduleCommand> localizer)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
                _contextAccessor = contextAccessorFactory();
                _localizer = localizer;
            }

            public async Task<Unit> Handle(UpdateWorkScheduleCommand request, CancellationToken cancellationToken)
            {
                var model = request.Model;
                Log.Information($"Update WorkSchedule Id: {request.Id} with data: {JsonSerializer.Serialize(model)}");

                var entity = await _dataContext.VcWorkSchedules.FindAsync(request.Id);

                if (entity == null || !entity.IsActive)
                {
                    throw new KeyNotFoundException(_localizer["work_schedule.not_found"]);
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

                // 3. Validate for schedule conflicts (excluding the current schedule itself)
                var conflictExists = await _dataContext.VcWorkSchedules
                    .AnyAsync(ws =>
                        ws.Id != request.Id && // Exclude the current entity
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

                model.UpdateEntity(entity);
                entity.ModifiedUserId = _contextAccessor.UserId;

                _dataContext.VcWorkSchedules.Update(entity);
                await _dataContext.SaveChangesAsync(cancellationToken);

                // Remove cache
                _cacheService.Remove(WorkScheduleConstant.BuildCacheKey(string.Empty));
                _cacheService.Remove(WorkScheduleConstant.BuildCacheKey(entity.Id.ToString()));

                Log.Information($"WorkSchedule with Id: {entity.Id} updated successfully.");

                return Unit.Value;
            }
        }
    }
}
