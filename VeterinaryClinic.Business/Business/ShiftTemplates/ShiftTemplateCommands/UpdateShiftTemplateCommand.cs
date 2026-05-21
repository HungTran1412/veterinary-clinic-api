using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class UpdateShiftTemplateCommand : IRequest<Unit>
    {
        public UpdateShiftTemplateModel Model { get; }

        public UpdateShiftTemplateCommand(UpdateShiftTemplateModel model)
        {
            Model = model;
        }

        public class Handler : IRequestHandler<UpdateShiftTemplateCommand, Unit>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly ICacheService _cacheService;
            private readonly IStringLocalizer<UpdateShiftTemplateCommand> _localizer;
            private readonly IContextAccessor _contextAccessor;

            public Handler(VeterinaryClinicDataContext dataContext, ICacheService cacheService, IStringLocalizer<UpdateShiftTemplateCommand> localizer, Func<IContextAccessor> contextAccessorFactory)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
                _localizer = localizer;
                _contextAccessor = contextAccessorFactory();
            }

            public async Task<Unit> Handle(UpdateShiftTemplateCommand request, CancellationToken cancellationToken)
            {
                if (_contextAccessor.Role != Role.ADMIN.ToString())
                {
                    throw new ArgumentException(_localizer["forbidden"]);
                }

                var model = request.Model;
                Log.Information($"Update {ShiftTemplateConstant.CachePrefix}: " + JsonSerializer.Serialize(model));
                
                var entity = await _dataContext.VcShiftTemplates.FirstOrDefaultAsync(x => x.Id == model.Id, cancellationToken);
                if (entity == null)
                {
                    throw new ArgumentException($"{_localizer["data.not_found"]}");
                }
                
                entity.ModifiedUserId = _contextAccessor.UserId;
                model.UpdateEntity(entity);
                
                 _dataContext.VcShiftTemplates.Update(entity);
                await _dataContext.SaveChangesAsync(cancellationToken);
                
                _cacheService.Remove(ShiftTemplateConstant.BuildCacheKey(entity.Id.ToString()));
                _cacheService.Remove(ShiftTemplateConstant.BuildCacheKey());
                
                return Unit.Value;
            }
        }
    }
}