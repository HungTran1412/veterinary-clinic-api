using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class CreateServiceCommand : IRequest<Unit>
    {
        public CreateServiceModel Model { get; }

        /// <summary>
        /// Them moi dich vu
        /// </summary>
        /// <param name="model">Thong tin dich vu can them</param>
        public CreateServiceCommand(CreateServiceModel model)
        {
            Model = model;
        }

        public class Handler : IRequestHandler<CreateServiceCommand, Unit>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly ICacheService _cacheService;
            private readonly IStringLocalizer<CreateServiceCommand> _localizer;
            private readonly IContextAccessor _contextAccessor;

            public Handler(VeterinaryClinicDataContext dataContext, ICacheService cacheService, IStringLocalizer<CreateServiceCommand> localizer, Func<IContextAccessor> contextAccessorFactory)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
                _localizer = localizer;
                _contextAccessor = contextAccessorFactory();
            }

            public async Task<Unit> Handle(CreateServiceCommand request, CancellationToken cancellationToken)
            {
                var model = request.Model;
                var currentUserId = _contextAccessor.UserId;
                var userRole = _contextAccessor.Role;
                Log.Information($"Create Service attempt by User {currentUserId}: {JsonSerializer.Serialize(model)}");

                // Security Check: Only ADMIN or RECEPTIONIST can create.
                if (userRole != Role.ADMIN.ToString())
                {
                    throw new ArgumentException(_localizer["user.unauthorized"]);
                }
                
                //map du lieu
                var entity = AutoMapperUtils.AutoMap<CreateServiceModel, VcServices>(model);
                
                if (entity == null)
                {
                    throw new ArgumentException("Failed to map data.");
                }

                //kiem tra ma trung
                var checkCode = await _dataContext.VcServices.AnyAsync(x => x.Code == entity.Code, cancellationToken);
                if (checkCode)
                {
                    throw new ArgumentException($"{_localizer["service.existed.code"]}");
                }

                //luu vao database
                await _dataContext.VcServices.AddAsync(entity, cancellationToken);
                await _dataContext.SaveChangesAsync(cancellationToken);
                
                //xoa cache
                _cacheService.Remove(ServiceConstant.BuildCacheKey());
                
                return Unit.Value;
            }
        }
    }   
}