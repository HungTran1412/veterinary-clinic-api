using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using VeterinaryClinic.Business;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class CreatePetCommand : IRequest<Unit>
    {
        public CreatePetModel Model { get; }

        /// <summary>
        /// Thêm mới thú cưng
        /// </summary>
        /// <param name="model">Thông tin thú cưng cần thêm mới</param>
        public CreatePetCommand(CreatePetModel model)
        {
            Model = model;
        }

        public class Handler : IRequestHandler<CreatePetCommand, Unit>
        {
            private readonly VeterinaryClinicDbContext _dataContext;
            private readonly ICacheService _cacheService;
            private readonly IStringLocalizer<CreatePetCommand> _localizer;
            
            // Giả lập Resources class cho localizer nếu cần, ở đây dùng chính class Command làm resource type
            public Handler(VeterinaryClinicDbContext dataContext, ICacheService cacheService, IStringLocalizer<CreatePetCommand> localizer)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
                _localizer = localizer;
            }

            public async Task<Unit> Handle(CreatePetCommand request, CancellationToken cancellationToken)
            {
                var model = request.Model;
                Log.Information($"Create Pet: " + JsonSerializer.Serialize(model));

                // Map từ Model sang Entity
                var entity = AutoMapperUtils.AutoMap<CreatePetModel, Pet>(model);

                // Validate logic (ví dụ: check trùng tên và chủ sở hữu)
                var checkExist = await _dataContext.Pets.AnyAsync(x => x.Name == entity.Name && x.OwnerName == entity.OwnerName, cancellationToken);
                if (checkExist)
                {
                    // Giả định key resource
                    throw new ArgumentException($"{_localizer["Pet.existed"]}");
                }

                await _dataContext.Pets.AddAsync(entity, cancellationToken);
                await _dataContext.SaveChangesAsync(cancellationToken);

                // Log action (giả lập)
                Log.Information($"Created Pet: {entity.Id} - {entity.Name}");

                // Xóa cache liên quan
                _cacheService.Remove("Pets_GetAll");

                return Unit.Value;
            }
        }
    }    
}