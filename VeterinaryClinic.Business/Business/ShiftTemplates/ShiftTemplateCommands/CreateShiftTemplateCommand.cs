using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class CreateShiftTemplateCommand : IRequest<Unit>
    {
        public CreateShiftTemplateModel Model { get; }

        public CreateShiftTemplateCommand(CreateShiftTemplateModel model)
        {
            Model = model;
        }

        public class Handler : IRequestHandler<CreateShiftTemplateCommand, Unit>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly ICacheService _cacheService;
            private readonly IStringLocalizer<CreateShiftTemplateCommand> _localizer;
            private readonly IContextAccessor _contextAccessor;

            public Handler(VeterinaryClinicDataContext dataContext, ICacheService cacheService, IStringLocalizer<CreateShiftTemplateCommand> localizer, Func<IContextAccessor> contextAccessorFactory)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
                _localizer = localizer;
                _contextAccessor = contextAccessorFactory();
            }

            public async Task<Unit> Handle(CreateShiftTemplateCommand request, CancellationToken cancellationToken)
            {
                if (_contextAccessor.Role != Role.ADMIN.ToString())
                {
                    throw new ArgumentException(_localizer["forbidden"]);
                }

                var model = request.Model;
                Log.Information($"Create ShiftTemplate: " + JsonSerializer.Serialize(model));
                
                var entity = AutoMapperUtils.AutoMap<CreateShiftTemplateModel, VcShiftTemplates>(model);
                
                if (entity == null)
                {
                    throw new ArgumentException(_localizer["data.not_found"]);
                }

                var checkCode = await _dataContext.VcShiftTemplates.AnyAsync(x => x.Code == entity.Code, cancellationToken);
                if (checkCode)
                {
                    throw new ArgumentException($"{_localizer["ShiftTemplate.existed.code"]}");
                }

                entity.CreatedUserId = _contextAccessor.UserId;
                await _dataContext.VcShiftTemplates.AddAsync(entity, cancellationToken);
                await _dataContext.SaveChangesAsync(cancellationToken);
                
                _cacheService.Remove(ShiftTemplateConstant.BuildCacheKey());
                
                return Unit.Value;
            }
        }
    }
}