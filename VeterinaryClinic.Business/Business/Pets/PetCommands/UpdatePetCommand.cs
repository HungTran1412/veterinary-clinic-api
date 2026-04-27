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
    public class UpdatePetCommand : IRequest<Unit>
    {
        public int Id { get; }
        public UpdatePetModel Model { get; }

        public UpdatePetCommand(int id, UpdatePetModel model)
        {
            Id = id;
            Model = model;
        }

        public class Handler : IRequestHandler<UpdatePetCommand, Unit>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly IContextAccessor _contextAccessor;
            private readonly IStringLocalizer<UpdatePetCommand> _localizer;

            public Handler(VeterinaryClinicDataContext dataContext, Func<IContextAccessor> contextAccessorFactory, IStringLocalizer<UpdatePetCommand> localizer)
            {
                _dataContext = dataContext;
                _contextAccessor = contextAccessorFactory();
                _localizer = localizer;
            }

            public async Task<Unit> Handle(UpdatePetCommand request, CancellationToken cancellationToken)
            {
                var model = request.Model;
                var currentUserId = _contextAccessor.UserId;
                var userRole = _contextAccessor.Role;
                Log.Information($"Update Pet {request.Id} attempt by User {currentUserId}: {JsonSerializer.Serialize(model)}");

                // Validate BirthDate
                if (model.BirthDate > DateTime.UtcNow)
                {
                    throw new ArgumentException(_localizer["pet.birthdate.future"]);
                }

                var entity = await _dataContext.VcPets.FindAsync(request.Id);
                if (entity == null || !entity.IsActive)
                {
                    throw new ArgumentException(_localizer["pet.not_found"]);
                }

                // Security Check: Only ADMIN, RECEPTIONIST, or the pet's owner can update.
                if (userRole != Role.RECEPTIONIST.ToString() && entity.OwnerId != currentUserId)
                {
                    throw new ArgumentException(_localizer["user.unauthorized"]);
                }

                // Business Logic: Prevent changing the owner.
                if (model.OwnerId != entity.OwnerId)
                {
                    throw new ArgumentException(_localizer["pet.update.cannot_change_owner"]);
                }

                // Update the entity
                model.UpdateEntity(entity);
                _dataContext.VcPets.Update(entity);
                await _dataContext.SaveChangesAsync(cancellationToken);

                Log.Information($"Pet {request.Id} updated successfully.");
                return Unit.Value;
            }
        }
    }
}
