using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class UpdateServiceCommand : IRequest<Unit>
    {
        public UpdateServiceModel Model { get; }

        /// <summary>
        /// Cap nhat dich vu
        /// </summary>
        /// <param name="model">Thong tin dich vu can cap nhat</param>
        public UpdateServiceCommand(UpdateServiceModel model)
        {
            Model = model;
        }

        public class Handler : IRequestHandler<UpdateServiceCommand, Unit>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly ICacheService _cacheService;
            private readonly IStringLocalizer<UpdateServiceCommand> _localizer;
            private readonly IContextAccessor _contextAccessor;

            public Handler(VeterinaryClinicDataContext dataContext, ICacheService cacheService, IStringLocalizer<UpdateServiceCommand> localizer, Func<IContextAccessor> contextAccessorFactory)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
                _localizer = localizer;
                _contextAccessor = contextAccessorFactory();
            }

            public async Task<Unit> Handle(UpdateServiceCommand request, CancellationToken cancellationToken)
            {
                var model = request.Model;
                Log.Information($"Update {ServiceConstant.CachePrefix}: " + JsonSerializer.Serialize(model));
                
                //Kiem tra ton tai khong
                var entity = await _dataContext.VcServices.FirstOrDefaultAsync(x => x.Id == model.Id);
                if (entity == null)
                {
                    throw new ArgumentException($"{_localizer["data.not-found"]}");
                }
                
                //cap nhat entity
                entity.ModifiedUserId = _contextAccessor.UserId;
                model.UpdateEntity(entity);
                
                //luu vao db
                 _dataContext.VcServices.Update(entity);
                await _dataContext.SaveChangesAsync(cancellationToken);
                
                //xoa cache
                _cacheService.Remove(ServiceConstant.BuildCacheKey(entity.Id.ToString()));
                _cacheService.Remove(ServiceConstant.BuildCacheKey());
                
                return Unit.Value;
            }
        }
    }   
}