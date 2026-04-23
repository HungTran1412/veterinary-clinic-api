using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using System.Text.Json;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using VeterinaryClinic.Shared.ContextAccessor;

namespace VeterinaryClinic.Business
{
    public class CreatePetCommand : IRequest<int>
    {
        public CreatePetModel Model { get; }

        public CreatePetCommand(CreatePetModel model)
        {
            Model = model;
        }

        public class Handler : IRequestHandler<CreatePetCommand, int>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly IContextAccessor _contextAccessor;
            private readonly IStringLocalizer<CreatePetCommand> _localizer;

            public Handler(VeterinaryClinicDataContext dataContext, Func<IContextAccessor> contextAccessorFactory, IStringLocalizer<CreatePetCommand> localizer)
            {
                _dataContext = dataContext;
                _contextAccessor = contextAccessorFactory();
                _localizer = localizer;
            }

            public async Task<int> Handle(CreatePetCommand request, CancellationToken cancellationToken)
            {
                var model = request.Model;
                var currentUserId = _contextAccessor.UserId;
                var userRole = _contextAccessor.Role;
                Log.Information($"Create Pet attempt by User {currentUserId}: {JsonSerializer.Serialize(model)}");

                // Validation 1: The creator must be CUSTOMER or RECEPTIONIST
                if (userRole != Role.CUSTOMER.ToString() && userRole != Role.RECEPTIONIST.ToString())
                {
                    throw new UnauthorizedAccessException(_localizer["pet.create.unauthorized"]);
                }

                // Validation 2: If the creator is a CUSTOMER, they can only create pets for themselves.
                if (userRole == Role.CUSTOMER.ToString() && model.OwnerId != currentUserId)
                {
                    throw new UnauthorizedAccessException(_localizer["pet.create.cannot_create_for_others"]);
                }

                // Validation 3: The specified owner must exist and must be a CUSTOMER.
                var owner = await _dataContext.VcUsers.FindAsync(model.OwnerId);
                if (owner == null || owner.Role != Role.CUSTOMER.ToString())
                {
                    throw new KeyNotFoundException(_localizer["pet.owner.not_found_or_invalid"]);
                }

                var entity = new VcPets
                {
                    Code = GenerateCodeUtils.GenerateUserCode("PET"),
                    Name = model.Name,
                    Species = model.Species,
                    Breed = model.Breed,
                    Gender = model.Gender,
                    IsNeutered = model.IsNeutered,
                    BirthDate = model.BirthDate,
                    Weight = model.Weight,
                    Color = model.Color,
                    ImageUrl = model.ImageUrl ?? string.Empty,
                    OwnerId = model.OwnerId,
                    Note = model.Note ?? string.Empty,
                    IsActive = true,
                    Order = model.Order,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUserId = currentUserId,
                    CreatedUserName = _contextAccessor.UserName
                };

                await _dataContext.VcPets.AddAsync(entity, cancellationToken);
                await _dataContext.SaveChangesAsync(cancellationToken);

                Log.Information($"Pet created successfully with Id: {entity.Id}");
                return entity.Id;
            }
        }
    }
}
