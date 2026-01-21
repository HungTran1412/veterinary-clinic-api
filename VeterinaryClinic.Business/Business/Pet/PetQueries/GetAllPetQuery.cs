using MediatR;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Business;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business;

public class GetAllPetQuery : IRequest<List<PetModal>>
{
    public class Handler : IRequestHandler<GetAllPetQuery, List<PetModal>>
    {
        private readonly VeterinaryClinicDbContext _context;
        private readonly ICacheService _cacheService;

        public Handler(VeterinaryClinicDbContext context, ICacheService cacheService)
        {
            _context = context;
            _cacheService = cacheService;
        }

        public async Task<List<PetModal>> Handle(GetAllPetQuery request, CancellationToken cancellationToken)
        {
            // Sử dụng cache với key "Pets_GetAll"
            return await _cacheService.GetOrCreate("Pets_GetAll", async () =>
            {
                var pets = await _context.Pets.ToListAsync(cancellationToken);
                // Map Entity sang DTO
                return AutoMapperUtils.AutoMap<List<Pet>, List<PetModal>>(pets);
            });
        }
    }
}