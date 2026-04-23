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

            public Handler(VeterinaryClinicDataContext dataContext, Func<IContextAccessor> contextAccessorFactory, IStringLocalizer<DeletePetCommand> localizer)
            {
                _dataContext = dataContext;
                _contextAccessor = contextAccessorFactory();
                _localizer = localizer;
            }

            public async Task<Unit> Handle(DeletePetCommand request, CancellationToken cancellationToken)
            {
                var currentUserId = _contextAccessor.UserId;
                var userRole = _contextAccessor.Role;
                Log.Information($"Delete Pet {request.Id} attempt by User {currentUserId}");

                var entity = await _dataContext.VcPets.FindAsync(request.Id);
                if (entity == null || !entity.IsActive)
                {
                    throw new KeyNotFoundException(_localizer["pet.not_found"]);
                }

                // Security Check: Only RECEPTIONIST or the pet's owner can delete.
                if (userRole != Role.RECEPTIONIST.ToString() && entity.OwnerId != currentUserId)
                {
                    throw new UnauthorizedAccessException(_localizer["user.unauthorized"]);
                }

                entity.IsActive = false; // Soft delete
                entity.ModifiedUserId = currentUserId;
                entity.ModifiedDate = DateTime.UtcNow;
                entity.ModifiedUserName = _contextAccessor.UserName;

                _dataContext.VcPets.Update(entity);
                await _dataContext.SaveChangesAsync(cancellationToken);

                Log.Information($"Pet {request.Id} deleted (soft) successfully.");
                return Unit.Value;
            }
        }
    }
}
