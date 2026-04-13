using MediatR;
using Microsoft.Extensions.Localization;
using Serilog;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using VeterinaryClinic.Shared.ContextAccessor;

namespace VeterinaryClinic.Business
{
    public class DeleteWorkScheduleCommand : IRequest<Unit>
    {
        public int Id { get; }

        public DeleteWorkScheduleCommand(int id)
        {
            Id = id;
        }

        public class Handler : IRequestHandler<DeleteWorkScheduleCommand, Unit>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly ICacheService _cacheService;
            private readonly IContextAccessor _contextAccessor;
            private readonly IStringLocalizer<DeleteWorkScheduleCommand> _localizer;

            public Handler(VeterinaryClinicDataContext dataContext, ICacheService cacheService, Func<IContextAccessor> contextAccessorFactory, IStringLocalizer<DeleteWorkScheduleCommand> localizer)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
                _contextAccessor = contextAccessorFactory();
                _localizer = localizer;
            }

            public async Task<Unit> Handle(DeleteWorkScheduleCommand request, CancellationToken cancellationToken)
            {
                Log.Information($"Delete WorkSchedule Id: {request.Id}");

                var entity = await _dataContext.VcWorkSchedules.FindAsync(request.Id);

                if (entity == null)
                {
                    throw new KeyNotFoundException(_localizer["work_schedule.not_found"]);
                }

                entity.IsActive = false;
                // entity.ModifiedUserId = _contextAccessor.GetUserId();

                _dataContext.VcWorkSchedules.Update(entity);
                await _dataContext.SaveChangesAsync(cancellationToken);

                // Remove cache
                _cacheService.Remove(WorkScheduleConstant.BuildCacheKey(string.Empty));
                _cacheService.Remove(WorkScheduleConstant.BuildCacheKey(entity.Id.ToString()));

                Log.Information($"WorkSchedule with Id: {entity.Id} deleted (soft delete) successfully.");

                return Unit.Value;
            }
        }
    }
}
