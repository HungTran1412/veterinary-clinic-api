using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using VeterinaryClinic.Shared.ContextAccessor;

namespace VeterinaryClinic.Business;

public class ProcessWorkScheduleRegistrationCommand : IRequest<Unit>
{
    public int Id { get; }
    public ProcessWorkScheduleRegistrationModel Model { get; }

    public ProcessWorkScheduleRegistrationCommand(int id, ProcessWorkScheduleRegistrationModel model)
    {
        Id = id;
        Model = model;
    }

    public class Handler : IRequestHandler<ProcessWorkScheduleRegistrationCommand, Unit>
    {
        private readonly VeterinaryClinicDataContext _dataContext;
        private readonly ICacheService _cacheService;
        private readonly IContextAccessor _contextAccessor;
        private readonly IStringLocalizer<ProcessWorkScheduleRegistrationCommand> _localizer;
        private readonly IMediator _mediator;

        public Handler(
            VeterinaryClinicDataContext dataContext,
            ICacheService cacheService,
            Func<IContextAccessor> contextAccessorFactory,
            IStringLocalizer<ProcessWorkScheduleRegistrationCommand> localizer,
            IMediator mediator)
        {
            _dataContext = dataContext;
            _cacheService = cacheService;
            _contextAccessor = contextAccessorFactory();
            _localizer = localizer;
            _mediator = mediator;
        }

        public async Task<Unit> Handle(ProcessWorkScheduleRegistrationCommand request, CancellationToken cancellationToken)
        {
            if (!Enum.TryParse<WorkScheduleRegisterStatus>(request.Model.Status, true, out var nextStatus))
            {
                throw new ArgumentException(_localizer["data.not_found"]);
            }

            var entity = await _dataContext.VcWorkScheduleRegistrations
                .FirstOrDefaultAsync(x => x.Id == request.Id && x.IsActive, cancellationToken);
            if (entity == null)
            {
                throw new ArgumentException(_localizer["data.not_found"]);
            }

            if (!Enum.TryParse<WorkScheduleRegisterStatus>(entity.Status, true, out var currentStatus))
            {
                throw new ArgumentException("Invalid work schedule registration status.");
            }

            ValidatePermission(entity, nextStatus);
            ValidateTransition(currentStatus, nextStatus);

            await using var transaction = await _dataContext.Database.BeginTransactionAsync(cancellationToken);

            entity.Status = nextStatus.ToString();
            entity.Note = request.Model.Note ?? entity.Note ?? string.Empty;
            entity.ModifiedDate = DateTime.UtcNow;
            entity.ModifiedUserId = _contextAccessor.UserId;
            entity.ModifiedUserName = _contextAccessor.UserName;

            await _dataContext.SaveChangesAsync(cancellationToken);

            if (nextStatus == WorkScheduleRegisterStatus.APPROVED)
            {
                var shiftTemplate = await _dataContext.VcShiftTemplates
                    .FirstOrDefaultAsync(x => x.Id == entity.ShiftTemplateId && x.IsActive, cancellationToken);
                if (shiftTemplate == null)
                {
                    throw new ArgumentException("Shift template not found.");
                }

                var workDate = entity.WorkDate.Date;
                var startTime = workDate.Add(shiftTemplate.StartTime.ToTimeSpan());
                var endTime = workDate.Add(shiftTemplate.EndTime.ToTimeSpan());

                await _mediator.Send(
                    new CreateManyWorkScheduleCommand(new List<CreateWorkScheduleModel>
                    {
                        new()
                        {
                            UserId = entity.UserId,
                            WorkDate = workDate,
                            StartTime = startTime,
                            EndTime = endTime,
                            ShiftName = shiftTemplate.ShiftName,
                            Note = entity.Note
                        }
                    }),
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            _cacheService.Remove(WorkScheduleRegistrationConstant.BuildCacheKey());
            _cacheService.Remove(WorkScheduleRegistrationConstant.BuildCacheKey(entity.Id.ToString()));

            Log.Information(
                "WorkScheduleRegistration {RegistrationId} processed from {CurrentStatus} to {NextStatus}",
                entity.Id,
                currentStatus,
                nextStatus);

            return Unit.Value;
        }

        private void ValidatePermission(VcWorkScheduleRegistrations entity, WorkScheduleRegisterStatus nextStatus)
        {
            var role = _contextAccessor.Role;
            var userId = _contextAccessor.UserId;
            var isAdmin = role == Role.ADMIN.ToString();
            var isOwner = userId.HasValue && entity.UserId == userId.Value;

            if (nextStatus == WorkScheduleRegisterStatus.APPROVED || nextStatus == WorkScheduleRegisterStatus.REJECTED)
            {
                if (!isAdmin)
                {
                    throw new UnauthorizedAccessException(_localizer["user.unauthorized"]);
                }

                return;
            }

            if (nextStatus == WorkScheduleRegisterStatus.CANCELED)
            {
                if (!isAdmin && !isOwner)
                {
                    throw new UnauthorizedAccessException(_localizer["user.unauthorized"]);
                }

                return;
            }

            throw new UnauthorizedAccessException(_localizer["user.unauthorized"]);
        }

        private static void ValidateTransition(WorkScheduleRegisterStatus currentStatus, WorkScheduleRegisterStatus nextStatus)
        {
            if (nextStatus == WorkScheduleRegisterStatus.PENDING)
            {
                throw new InvalidOperationException("Cannot move registration back to pending.");
            }

            if (currentStatus != WorkScheduleRegisterStatus.PENDING)
            {
                throw new InvalidOperationException("Only pending registration can be processed.");
            }
        }
    }
}
