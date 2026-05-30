using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using VeterinaryClinic.Shared.ContextAccessor;

namespace VeterinaryClinic.Business;

public class ProcessManyWorkScheduleRegistrationCommand : IRequest<Unit>
{
    public ProcessManyWorkScheduleRegistrationModel Model { get; }

    public ProcessManyWorkScheduleRegistrationCommand(ProcessManyWorkScheduleRegistrationModel model)
    {
        Model = model;
    }

    public class Handler : IRequestHandler<ProcessManyWorkScheduleRegistrationCommand, Unit>
    {
        private readonly VeterinaryClinicDataContext _dataContext;
        private readonly ICacheService _cacheService;
        private readonly IContextAccessor _contextAccessor;
        private readonly IStringLocalizer<ProcessManyWorkScheduleRegistrationCommand> _localizer;
        private readonly IMediator _mediator;
        private readonly INotificationService _notificationService;

        public Handler(
            VeterinaryClinicDataContext dataContext,
            ICacheService cacheService,
            Func<IContextAccessor> contextAccessorFactory,
            IStringLocalizer<ProcessManyWorkScheduleRegistrationCommand> localizer,
            IMediator mediator,
            INotificationService notificationService)
        {
            _dataContext = dataContext;
            _cacheService = cacheService;
            _contextAccessor = contextAccessorFactory();
            _localizer = localizer;
            _mediator = mediator;
            _notificationService = notificationService;
        }

        public async Task<Unit> Handle(ProcessManyWorkScheduleRegistrationCommand request, CancellationToken cancellationToken)
        {
            var model = request.Model;
            var currentUserId = _contextAccessor.UserId;
            var currentUserRole = _contextAccessor.Role;

            Log.Information("Processing many WorkScheduleRegistrations by User {UserId}", currentUserId);

            // 1. Validate input status and permissions
            if (!Enum.TryParse<WorkScheduleRegisterStatus>(model.Status, true, out var nextStatus) ||
                (nextStatus != WorkScheduleRegisterStatus.APPROVED && nextStatus != WorkScheduleRegisterStatus.REJECTED))
            {
                throw new ArgumentException(_localizer["work-schedule-registration.invalid_process_status"]);
            }

            if (currentUserRole != Role.ADMIN.ToString())
            {
                throw new UnauthorizedAccessException(_localizer["user.unauthorized"]);
            }

            // 2. Fetch all entities in one DB call
            var registrations = await _dataContext.VcWorkScheduleRegistrations
                .Where(x => model.RegistrationIds.Contains(x.Id) && x.IsActive)
                .ToListAsync(cancellationToken);

            if (!registrations.Any())
            {
                throw new ArgumentException(_localizer["data.not_found"]);
            }

            var workSchedulesToCreate = new List<CreateWorkScheduleModel>();
            
            await using var transaction = await _dataContext.Database.BeginTransactionAsync(cancellationToken);

            // 3. Process each entity
            foreach (var entity in registrations)
            {
                // Validate current state
                if (entity.Status != WorkScheduleRegisterStatus.PENDING.ToString())
                {
                    Log.Warning("Skipping registration {RegistrationId} as its status is not PENDING.", entity.Id);
                    continue; // Skip if not in PENDING state
                }

                // Update entity
                entity.Status = nextStatus.ToString();
                entity.Note = model.Note ?? entity.Note ?? string.Empty;
                entity.ModifiedDate = DateTime.UtcNow;
                entity.ModifiedUserId = currentUserId;
                entity.ModifiedUserName = _contextAccessor.UserName;

                // If approved, prepare to create a work schedule
                if (nextStatus == WorkScheduleRegisterStatus.APPROVED)
                {
                    var shiftTemplate = await _dataContext.VcShiftTemplates
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == entity.ShiftTemplateId, cancellationToken);
                    
                    if (shiftTemplate != null)
                    {
                        var workDate = entity.WorkDate.Date;
                        workSchedulesToCreate.Add(new CreateWorkScheduleModel
                        {
                            UserId = entity.UserId,
                            WorkDate = workDate,
                            StartTime = workDate.Add(shiftTemplate.StartTime.ToTimeSpan()),
                            EndTime = workDate.Add(shiftTemplate.EndTime.ToTimeSpan()),
                            ShiftName = shiftTemplate.ShiftName,
                            Note = entity.Note
                        });
                    }
                }
                
                // Send notification to the doctor
                string title = "Kết quả đăng ký lịch làm việc";
                string message = nextStatus == WorkScheduleRegisterStatus.APPROVED
                    ? $"Đăng ký lịch làm việc của bạn cho ngày {entity.WorkDate:dd/MM/yyyy} đã được chấp thuận."
                    : $"Đăng ký lịch làm việc của bạn cho ngày {entity.WorkDate:dd/MM/yyyy} đã bị từ chối.";

                var notification = new NotificationModel
                {
                    UserId = entity.UserId,
                    Title = title,
                    Message = message,
                    Type = NotificationType.MESSAGE.ToString(),
                    RelatedEntityId = entity.Id,
                    RelatedEntityType = RelatedEntityType.WorkScheduleRegistration.ToString()
                };
                await _notificationService.SendAndSaveNotificationAsync(notification);
            }

            // 4. Create all work schedules in a single batch if any
            if (workSchedulesToCreate.Any())
            {
                await _mediator.Send(new CreateManyWorkScheduleCommand(workSchedulesToCreate), cancellationToken);
            }

            // 5. Save all changes to the database
            await _dataContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // 6. Invalidate cache
            _cacheService.Remove(WorkScheduleRegistrationConstant.BuildCacheKey());
            foreach (var id in model.RegistrationIds)
            {
                _cacheService.Remove(WorkScheduleRegistrationConstant.BuildCacheKey(id.ToString()));
            }

            Log.Information("Successfully processed {Count} work schedule registrations to status {Status}", registrations.Count, nextStatus);

            return Unit.Value;
        }
    }
}
