using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using VeterinaryClinic.Shared.ContextAccessor;

namespace VeterinaryClinic.Business
{
    public class CreateSpecializationCommand : IRequest<Unit>
    {
        public CreateSpecializationModel Model { get; }

        /// <summary>
        /// Thêm mới chuyên ngành
        /// </summary>
        /// <param name="model">Thông tin chuyên ngành cần thêm mới</param>
        public CreateSpecializationCommand(CreateSpecializationModel model)
        {
            Model = model;
        }

        public class Handler : IRequestHandler<CreateSpecializationCommand, Unit>
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

            public async Task<Unit> Handle(CreateSpecializationCommand request, CancellationToken cancellationToken)
            {
                var model = request.Model;
                Log.Information($"Create {SpecializationConstant.CachePrefix}: " + JsonSerializer.Serialize(model));
                
                //map du lieu
                var entity = AutoMapperUtils.AutoMap<CreateSpecializationModel, VcSpecializations>(model);

                //kiem tra ma trung
                var checkCode = await _dataContext.VcSpecializations.AnyAsync(x => x.Code == entity.Code, cancellationToken);
                if (checkCode)
                {
                    throw new ArgumentException($"{_localizer["Specialization.existed.code;"]}");
                }

                //luu vao database
                await _dataContext.VcSpecializations.AddAsync(entity, cancellationToken);
                await _dataContext.SaveChangesAsync(cancellationToken);
                
                // Khôi phục lại các hằng số ban đầu
                _cacheService.Remove(SpecializationConstant.BuildCacheKey());
                
                return Unit.Value;
            }
        }
    }   
}