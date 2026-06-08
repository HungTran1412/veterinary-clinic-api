using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class CreateSpecializationCommand : IRequest<Unit>
    {
        public CreateSpecializationModel Model { get; }

        /// <summary>
        /// Them chuyen nganh
        /// </summary>
        /// <param name="model">Thong tin chuyen nganh can them</param>
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
                Log.Information($"Create Specialization: " + JsonSerializer.Serialize(model));
                
                //map du lieu
                var entity = AutoMapperUtils.AutoMap<CreateSpecializationModel, VcSpecializations>(model);
                
                // Validation cơ bản
                if (entity == null)
                {
                    throw new ArgumentException(_localizer["data.not_found"]);
                }

                //kiem tra ma trung
                var checkCode = await _dataContext.VcSpecializations.AnyAsync(x => x.Code == entity.Code, cancellationToken);
                if (checkCode)
                {
                    throw new ArgumentException($"{_localizer["Specialization.existed.code"]}");
                }

                var checkName = await _dataContext.VcSpecializations.AnyAsync(x => x.Name == entity.Name, cancellationToken);
                if (checkName)
                {
                    throw new ArgumentException($"{_localizer["Specialization.existed.name"]}");
                }
                
                //luu vao database
                entity.CreatedUserId = _contextAccessor.UserId;
                await _dataContext.VcSpecializations.AddAsync(entity, cancellationToken);
                await _dataContext.SaveChangesAsync(cancellationToken);
                
                //xoa cache
                _cacheService.Remove(SpecializationConstant.BuildCacheKey());
                
                return Unit.Value;
            }
        }
    }   
}