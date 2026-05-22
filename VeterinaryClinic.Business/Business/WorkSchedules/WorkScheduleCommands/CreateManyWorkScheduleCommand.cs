using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using System.Text.Json;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class CreateManyWorkScheduleCommand : IRequest<Unit>
    {
        public List<CreateWorkScheduleModel> ListModel { get; set; }

        public CreateManyWorkScheduleCommand(List<CreateWorkScheduleModel> listModel)
        {
            ListModel = listModel;
        }

        public class Handler : IRequestHandler<CreateManyWorkScheduleCommand, Unit>
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

            public async Task<Unit> Handle(CreateManyWorkScheduleCommand request, CancellationToken cancellationToken)
            {
                var listModel = request.ListModel;
                Log.Information($"Create many WorkSchedules: {JsonSerializer.Serialize(listModel)}");

                if (listModel == null || !listModel.Any())
                {
                    throw new ArgumentException(_localizer["data.not_found"]);
                }

                var entityAdds = new List<VcWorkSchedules>();
                var userCache = new Dictionary<int, VcUsers>();

                foreach (var model in listModel)
                {
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

                    // 2. Validate User exists and has correct role (with caching)
                    if (!userCache.TryGetValue(model.UserId, out var user))
                    {
                        user = await _dataContext.VcUsers.FindAsync(model.UserId);
                        if (user != null)
                        {
                            userCache[model.UserId] = user;
                        }
                    }

                    if (user == null)
                    {
                        throw new ArgumentException(_localizer["work_schedule.user.not_found"]);
                    }

                    var validRoles = new[] { Role.DOCTOR.ToString(), Role.RECEPTIONIST.ToString() };
                    if (!validRoles.Contains(user.Role))
                    {
                        throw new ArgumentException(_localizer["work_schedule.user.invalid_role"]);
                    }

                    // 3. Validate for schedule conflicts (both in DB and in the current batch)
                    var conflictInDb = await _dataContext.VcWorkSchedules
                        .AnyAsync(ws =>
                            ws.IsActive &&
                            ws.UserId == model.UserId &&
                            ws.WorkDate.Date == model.WorkDate.Date &&
                            model.StartTime < ws.EndTime &&
                            model.EndTime > ws.StartTime,
                            cancellationToken);

                    if (conflictInDb)
                    {
                        throw new ArgumentException($"{_localizer["work_schedule.schedule.conflict"]} - User: {_contextAccessor.UserName}, Date: {model.WorkDate.ToShortDateString()}");
                    }

                    var conflictInBatch = entityAdds
                        .Any(ws =>
                            ws.UserId == model.UserId &&
                            ws.WorkDate.Date == model.WorkDate.Date &&
                            model.StartTime < ws.EndTime &&
                            model.EndTime > ws.StartTime);

                    if (conflictInBatch)
                    {
                        throw new ArgumentException($"{_localizer["work_schedule.schedule.conflict"]} - User: {_contextAccessor.UserName}, Date: {model.WorkDate.ToShortDateString()} (within the same request)");
                    }
                    
                    var entity = new VcWorkSchedules
                    {
                        Code = GenerateCodeUtils.GenerateUserCode("WS"),
                        UserId = model.UserId,
                        WorkDate = model.WorkDate,
                        StartTime = model.StartTime,
                        EndTime = model.EndTime,
                        ShiftName = model.ShiftName,
                        Note = model.Note ?? string.Empty,
                    };

                    entityAdds.Add(entity);
                }

                await _dataContext.VcWorkSchedules.AddRangeAsync(entityAdds, cancellationToken);
                await _dataContext.SaveChangesAsync(cancellationToken);

                _cacheService.Remove(WorkScheduleConstant.BuildCacheKey());

                Log.Information($"Successfully created {entityAdds.Count} work schedules.");

                return Unit.Value;
            }
        }
    }
}
