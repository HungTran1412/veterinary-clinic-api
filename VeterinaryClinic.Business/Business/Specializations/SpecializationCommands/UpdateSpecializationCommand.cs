using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared.ContextAccessor;

namespace VeterinaryClinic.Business
{
    public class UpdateSpecializationCommand : IRequest<Unit>
    {
        public UpdateSpecializationModel Model { get; }

        /// <summary>
        /// Cập nhat chuyen nganh
        /// </summary>
        /// <param name="model">Thong tin cap nhat chuyen nganh</param>
        public UpdateSpecializationCommand(UpdateSpecializationModel model)
        {
            Model = model;
        }

        public class Handler : IRequestHandler<UpdateSpecializationCommand, Unit>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly ICacheService _cacheService;
            private readonly IStringLocalizer<CreateSpecializationCommand> _localizer;
            private readonly IContextAccessor _contextAccessor;

            public Handler(VeterinaryClinicDataContext dataContext, ICacheService cacheService, IStringLocalizer<CreateSpecializationCommand> localizer, Func<IContextAccessor> contextAccessorFactory)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
                _localizer = localizer;
                _contextAccessor = contextAccessorFactory();
            }

            public async Task<Unit> Handle(UpdateSpecializationCommand request, CancellationToken cancellationToken)
            {
                var model = request.Model;
                Log.Information($"Update {SpecializationConstant.CachePrefix}: " + JsonSerializer.Serialize(model));
                
                //Kiem tra ton tai khong
                var entity = await _dataContext.VcSpecializations.FirstOrDefaultAsync(x => x.Id == model.Id);
                if (entity == null)
                {
                    throw new ArgumentException($"{_localizer["data.not-found"]}");
                }
                
                //cap nhat entity
                model.UpdateEntity(entity);
                
                //luu vao db
                 _dataContext.VcSpecializations.Update(entity);
                await _dataContext.SaveChangesAsync();
                
                //xoa cache
                _cacheService.Remove(SpecializationConstant.BuildCacheKey(entity.Id.ToString()));
                _cacheService.Remove(SpecializationConstant.BuildCacheKey());
                
                return Unit.Value;
            }
        }
    }   
}