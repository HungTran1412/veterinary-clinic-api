using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using VeterinaryClinic.Shared.ContextAccessor;

namespace VeterinaryClinic.Business
{
    public class GetPetByIdQuery : IRequest<PetModel>
    {
        public int Id { get; }

        public GetPetByIdQuery(int id)
        {
            Id = id;
        }

        public class Handler : IRequestHandler<GetPetByIdQuery, PetModel>
        {
            private readonly VeterinaryClinicReadDataContext _dataContext;
            private readonly IContextAccessor _contextAccessor;
            private readonly IStringLocalizer<GetPetByIdQuery> _localizer;

            public Handler(VeterinaryClinicReadDataContext dataContext, Func<IContextAccessor> contextAccessorFactory, IStringLocalizer<GetPetByIdQuery> localizer)
            {
                _dataContext = dataContext;
                _contextAccessor = contextAccessorFactory();
                _localizer = localizer;
            }

            public async Task<PetModel> Handle(GetPetByIdQuery request, CancellationToken cancellationToken)
            {
                var pet = await _dataContext.VcPets
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == request.Id && p.IsActive, cancellationToken);

                if (pet == null)
                {
                    throw new KeyNotFoundException(_localizer["pet.not_found"]);
                }

                // Security check: Only owner, receptionist, or admin can view pet details
                var currentUserId = _contextAccessor.UserId;
                var userRole = _contextAccessor.Role;

                if (userRole != Role.ADMIN.ToString() && userRole != Role.RECEPTIONIST.ToString())
                {
                    if (!currentUserId.HasValue || pet.OwnerId != currentUserId.Value)
                    {
                        throw new UnauthorizedAccessException(_localizer["user.unauthorized"]);
                    }
                }
                
                var owner = await _dataContext.VcUsers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == pet.OwnerId, cancellationToken);

                var petModel = new PetModel
                {
                    Id = pet.Id,
                    Code = pet.Code,
                    Name = pet.Name,
                    Species = pet.Species,
                    Breed = pet.Breed,
                    Gender = pet.Gender,
                    IsNeutered = pet.IsNeutered,
                    BirthDate = pet.BirthDate,
                    Weight = pet.Weight,
                    Color = pet.Color,
                    ImageUrl = pet.ImageUrl,
                    OwnerId = pet.OwnerId,
                    OwnerName = owner?.FullName, // Populate owner name
                    Note = pet.Note,
                    IsActive = pet.IsActive,
                    Order = pet.Order,
                    CreatedDate = pet.CreatedDate
                };

                return petModel;
            }
        }
    }
}
