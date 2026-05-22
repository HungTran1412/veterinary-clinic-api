using MediatR;
using Microsoft.Extensions.Localization;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using VeterinaryClinic.Shared.ContextAccessor;

namespace VeterinaryClinic.Business;

public class DeleteWorkScheduleRegistrationCommand : IRequest<Unit>
{
    public int Id { get; }

    public DeleteWorkScheduleRegistrationCommand(int id)
    {
        Id = id;
    }

    public class Handler : IRequestHandler<DeleteWorkScheduleRegistrationCommand, Unit>
    {
        private readonly VeterinaryClinicDataContext _dataContext;
        private readonly ICacheService _cacheService;
        private readonly IContextAccessor _contextAccessor;
        private readonly IStringLocalizer<DeleteWorkScheduleRegistrationCommand> _localizer;

        public Handler(
            VeterinaryClinicDataContext dataContext,
            ICacheService cacheService,
            Func<IContextAccessor> contextAccessorFactory,
            IStringLocalizer<DeleteWorkScheduleRegistrationCommand> localizer)
        {
            _dataContext = dataContext;
            _cacheService = cacheService;
            _contextAccessor = contextAccessorFactory();
            _localizer = localizer;
        }

        public async Task<Unit> Handle(DeleteWorkScheduleRegistrationCommand request, CancellationToken cancellationToken)
        {
            var entity = await _dataContext.VcWorkScheduleRegistrations.FindAsync(new object[] { request.Id }, cancellationToken);
            if (entity == null || !entity.IsActive)
            {
                throw new ArgumentException(_localizer["data.not_found"]);
            }

            if (_contextAccessor.Role != Role.ADMIN.ToString() && entity.UserId != _contextAccessor.UserId)
            {
                throw new UnauthorizedAccessException(_localizer["user.unauthorized"]);
            }

            if (entity.Status == WorkScheduleRegisterStatus.APPROVED.ToString())
            {
                throw new ArgumentException("Approved registration cannot be deleted.");
            }

            entity.IsActive = false;
            entity.ModifiedDate = DateTime.UtcNow;
            entity.ModifiedUserId = _contextAccessor.UserId;
            entity.ModifiedUserName = _contextAccessor.UserName;

            await _dataContext.SaveChangesAsync(cancellationToken);

            _cacheService.Remove(WorkScheduleRegistrationConstant.BuildCacheKey());
            _cacheService.Remove(WorkScheduleRegistrationConstant.BuildCacheKey(entity.Id.ToString()));

            return Unit.Value;
        }
    }
}
