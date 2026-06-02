using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using System.Text.Json;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using VeterinaryClinic.Shared.ContextAccessor;

namespace VeterinaryClinic.Business;

public class CreateWorkScheduleRegistrationCommand : IRequest<int>
{
    public CreateWorkScheduleRegistrationModel Model { get; }

    public CreateWorkScheduleRegistrationCommand(CreateWorkScheduleRegistrationModel model)
    {
        Model = model;
    }

    public class Handler : IRequestHandler<CreateWorkScheduleRegistrationCommand, int>
    {
        private readonly VeterinaryClinicDataContext _dataContext;
        private readonly ICacheService _cacheService;
        private readonly IContextAccessor _contextAccessor;
        private readonly IStringLocalizer<CreateWorkScheduleRegistrationCommand> _localizer;
        private readonly INotificationService _notificationService;

        public Handler(
            VeterinaryClinicDataContext dataContext,
            ICacheService cacheService,
            Func<IContextAccessor> contextAccessorFactory,
            IStringLocalizer<CreateWorkScheduleRegistrationCommand> localizer,
            INotificationService notificationService)
        {
            _dataContext = dataContext;
            _cacheService = cacheService;
            _contextAccessor = contextAccessorFactory();
            _localizer = localizer;
            _notificationService = notificationService;
        }

        public async Task<int> Handle(CreateWorkScheduleRegistrationCommand request, CancellationToken cancellationToken)
        {
            var model = request.Model;
            Log.Information("Create WorkScheduleRegistration: {Payload}", JsonSerializer.Serialize(model));

            var currentRole = _contextAccessor.Role;
            var currentUserId = _contextAccessor.UserId;
            var isAdmin = currentRole == Role.ADMIN.ToString();
            var isDoctor = currentRole == Role.DOCTOR.ToString();
            var isReceptionist = currentRole == Role.RECEPTIONIST.ToString();

            if (!isAdmin && !isDoctor && !isReceptionist)
            {
                throw new UnauthorizedAccessException(_localizer["user.unauthorized"]);
            }

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

            var user = await _dataContext.VcUsers
                .FirstOrDefaultAsync(x => x.Id == targetUserId && x.IsActive, cancellationToken);
            if (user == null)
            {
                throw new ArgumentException(_localizer["work_schedule.user.not_found"]);
            }

            var validRoles = new[] { Role.DOCTOR.ToString(), Role.RECEPTIONIST.ToString() };
            if (!validRoles.Contains(user.Role))
            {
                throw new ArgumentException(_localizer["work_schedule.user.invalid_role"]);
            }

            var shiftTemplate = await _dataContext.VcShiftTemplates
                .FirstOrDefaultAsync(x => x.Id == model.ShiftTemplateId && x.IsActive, cancellationToken);
            if (shiftTemplate == null)
            {
                throw new ArgumentException(_localizer["data.not_found"]);
            }

            var duplicateRegistration = await _dataContext.VcWorkScheduleRegistrations
                .AnyAsync(x =>
                    x.IsActive &&
                    x.UserId == targetUserId &&
                    x.ShiftTemplateId == model.ShiftTemplateId &&
                    x.WorkDate.Date == model.WorkDate.Date &&
                    x.Status != WorkScheduleRegisterStatus.REJECTED.ToString() &&
                    x.Status != WorkScheduleRegisterStatus.CANCELED.ToString(),
                    cancellationToken);

            if (duplicateRegistration)
            {
                throw new ArgumentException(_localizer["work-schedule-registration.already_exists"]);
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

            await _dataContext.VcWorkScheduleRegistrations.AddAsync(registration, cancellationToken);
            await _dataContext.SaveChangesAsync(cancellationToken);

            #region Send Notification

            var title = "Đăng ký lịch làm việc mới";
            var message = $"{user.FullName} đã đăng ký một lịch làm việc mới vào ngày {registration.WorkDate:dd/MM/yyyy}.";
            
            var adminAndReceptionistIds = await _dataContext.VcUsers
                .Where(u => u.IsActive && (u.Role == Role.ADMIN.ToString() || u.Role == Role.RECEPTIONIST.ToString()))
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);

            foreach (var userId in adminAndReceptionistIds)
            {
                var notification = new NotificationModel
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = NotificationType.MESSAGE.ToString(),
                    RelatedEntityId = registration.Id,
                    RelatedEntityType = RelatedEntityType.WorkScheduleRegistration.ToString()
                };
                await _notificationService.SendAndSaveNotificationAsync(notification);
            }

            #endregion

            _cacheService.Remove(WorkScheduleRegistrationConstant.BuildCacheKey());

            return registration.Id;
        }
    }
}
