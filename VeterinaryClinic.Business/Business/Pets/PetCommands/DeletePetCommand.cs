using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using VeterinaryClinic.Shared.ContextAccessor;

namespace VeterinaryClinic.Business
{
    public class DeletePetCommand : IRequest<Unit>
    {
        public int Id { get; }

        public DeletePetCommand(int id)
        {
            Id = id;
        }

        public class Handler : IRequestHandler<DeletePetCommand, Unit>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly IContextAccessor _contextAccessor;
            private readonly IStringLocalizer<DeletePetCommand> _localizer;
            private readonly ICacheService _cacheService;

            public Handler(VeterinaryClinicDataContext dataContext, Func<IContextAccessor> contextAccessorFactory, IStringLocalizer<DeletePetCommand> localizer, ICacheService cacheService)
            {
                _dataContext = dataContext;
                _contextAccessor = contextAccessorFactory();
                _localizer = localizer;
                _cacheService = cacheService;
            }

            public async Task<Unit> Handle(DeletePetCommand request, CancellationToken cancellationToken)
            {
                var currentUserId = _contextAccessor.UserId;
                var userRole = _contextAccessor.Role;
                Log.Information($"Delete Pet {request.Id} attempt by User {currentUserId}");

                var entity = await _dataContext.VcPets.FindAsync(request.Id);
                if (entity == null || !entity.IsActive)
                {
                    throw new ArgumentException(_localizer["pet.not_found"]);
                }

                // Security Check: Only RECEPTIONIST or the pet's owner can delete.
                if (userRole != Role.RECEPTIONIST.ToString() && entity.OwnerId != currentUserId)
                {
                    throw new ArgumentException(_localizer["user.unauthorized"]);
                }

                entity.IsActive = false; // Soft delete

                _dataContext.VcPets.Update(entity);
                await _dataContext.SaveChangesAsync(cancellationToken);
                
                _cacheService.Remove(PetConstant.BuildCacheKey(entity.Id.ToString()));
                

                Log.Information($"Pet {request.Id} deleted (soft) successfully.");
                return Unit.Value;
            }
        }
    }
}
