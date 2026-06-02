using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using System.Text.Json;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class CreateManyWorkScheduleRegistrationCommand : IRequest<Unit>
    {
        public List<CreateWorkScheduleRegistrationModel> ListModel { get; set; }

        public CreateManyWorkScheduleRegistrationCommand(List<CreateWorkScheduleRegistrationModel> listModel)
        {
            ListModel = listModel;
        }

        public class Handler : IRequestHandler<CreateManyWorkScheduleRegistrationCommand, Unit>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly ICacheService _cacheService;
            private readonly IContextAccessor _contextAccessor;
            private readonly IStringLocalizer<CreateWorkScheduleRegistrationCommand> _localizer;

            public Handler(VeterinaryClinicDataContext dataContext, ICacheService cacheService, Func<IContextAccessor> contextAccessorFactory, IStringLocalizer<CreateWorkScheduleRegistrationCommand> localizer)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
                _contextAccessor = contextAccessorFactory();
                _localizer = localizer;
            }

            public async Task<Unit> Handle(CreateManyWorkScheduleRegistrationCommand request, CancellationToken cancellationToken)
            {
                var listModel = request.ListModel;
                Log.Information($"Create many WorkScheduleRegistrations: {JsonSerializer.Serialize(listModel)}");

                if (listModel == null || !listModel.Any())
                {
                    throw new ArgumentException(_localizer["data.not_found"]);
                }

                var entityAdds = new List<VcWorkScheduleRegistrations>();
                var userCache = new Dictionary<int, VcUsers>();
                var shiftTemplateCache = new Dictionary<int, VcShiftTemplates>();

                foreach (var model in listModel)
                {
                    var currentRole = _contextAccessor.Role;
                    var currentUserId = _contextAccessor.UserId;
                    var isAdmin = currentRole == Role.ADMIN.ToString();

                    var targetUserId = model.UserId;
                    if (!isAdmin)
                    {
                        if (!currentUserId.HasValue)
                        {
                            throw new UnauthorizedAccessException(_localizer["user.unauthorized"]);
                        }
                        targetUserId = currentUserId.Value;
                    }

                    if (model.WorkDate.Date < DateTime.Now.Date)
                    {
                        throw new ArgumentException(_localizer["work_schedule.work_date.cannot_be_in_the_past"]);
                    }

                    if (!userCache.TryGetValue(targetUserId, out var user))
                    {
                        user = await _dataContext.VcUsers.FirstOrDefaultAsync(x => x.Id == targetUserId && x.IsActive, cancellationToken);
                        if (user != null)
                        {
                            userCache[targetUserId] = user;
                        }
                    }

                    if (user == null)
                    {
                        throw new ArgumentException($"{_localizer["work_schedule.user.not_found"]} - ID: {targetUserId}");
                    }

                    var validRoles = new[] { Role.DOCTOR.ToString(), Role.RECEPTIONIST.ToString() };
                    if (!validRoles.Contains(user.Role))
                    {
                        throw new ArgumentException($"{_localizer["work_schedule.user.invalid_role"]} - User: {_contextAccessor.UserName}");
                    }

                    if (!shiftTemplateCache.TryGetValue(model.ShiftTemplateId, out var shiftTemplate))
                    {
                        shiftTemplate = await _dataContext.VcShiftTemplates.FirstOrDefaultAsync(x => x.Id == model.ShiftTemplateId && x.IsActive, cancellationToken);
                        if (shiftTemplate != null)
                        {
                            shiftTemplateCache[model.ShiftTemplateId] = shiftTemplate;
                        }
                    }
                    
                    if (shiftTemplate == null)
                    {
                        throw new ArgumentException($"{_localizer["data.not_found"]} - ShiftTemplateId: {model.ShiftTemplateId}");
                    }

                    var duplicateInDb = await _dataContext.VcWorkScheduleRegistrations
                        .AnyAsync(x =>
                            x.IsActive &&
                            x.UserId == targetUserId &&
                            x.ShiftTemplateId == model.ShiftTemplateId &&
                            x.WorkDate.Date == model.WorkDate.Date &&
                            x.Status != WorkScheduleRegisterStatus.REJECTED.ToString() &&
                            x.Status != WorkScheduleRegisterStatus.CANCELED.ToString(),
                            cancellationToken);

                    if (duplicateInDb)
                    {
                        throw new ArgumentException($"{_localizer["work-schedule-registration.already_exists"]} - User: {_contextAccessor.UserName}, Date: {model.WorkDate.ToShortDateString()}");
                    }

                    var duplicateInBatch = entityAdds
                        .Any(x =>
                            x.UserId == targetUserId &&
                            x.ShiftTemplateId == model.ShiftTemplateId &&
                            x.WorkDate.Date == model.WorkDate.Date);

                    if (duplicateInBatch)
                    {
                        throw new ArgumentException($"{_localizer["work-schedule-registration.already_exists"]} - User: {_contextAccessor.UserName}, Date: {model.WorkDate.ToShortDateString()} (within the same request)");
                    }

                    var registration = new VcWorkScheduleRegistrations
                    {
                        Code = GenerateCodeUtils.GenerateCode("WSR"),
                        UserId = targetUserId,
                        ShiftTemplateId = model.ShiftTemplateId,
                        WorkDate = model.WorkDate.Date,
                        Status = WorkScheduleRegisterStatus.PENDING.ToString(),
                        RegisteredDate = DateTime.UtcNow,
                        Note = model.Note ?? string.Empty,
                        IsActive = true,
                        Order = 0,
                        CreatedDate = DateTime.UtcNow,
                        CreatedUserId = _contextAccessor.UserId,
                        CreatedUserName = _contextAccessor.UserName
                    };
                    entityAdds.Add(registration);
                }

                await _dataContext.VcWorkScheduleRegistrations.AddRangeAsync(entityAdds, cancellationToken);
                await _dataContext.SaveChangesAsync(cancellationToken);

                _cacheService.Remove(WorkScheduleRegistrationConstant.BuildCacheKey());

                Log.Information($"Successfully created {entityAdds.Count} work schedule registrations.");

                return Unit.Value;
            }
        }
    }
}
