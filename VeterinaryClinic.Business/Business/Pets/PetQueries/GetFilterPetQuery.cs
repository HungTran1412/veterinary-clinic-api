using MediatR;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VeterinaryClinic.Business
{
    public class GetFilterPetQuery : IRequest<PaginationList<PetModel>>
    {
        public PetFilterModel Filter { get; }

        public GetFilterPetQuery(PetFilterModel filter)
        {
            Filter = filter;
        }

        public class Handler : IRequestHandler<GetFilterPetQuery, PaginationList<PetModel>>
        {
            private readonly VeterinaryClinicReadDataContext _dataContext;

            public Handler(VeterinaryClinicReadDataContext dataContext)
            {
                _dataContext = dataContext;
            }

            public async Task<PaginationList<PetModel>> Handle(GetFilterPetQuery request, CancellationToken cancellationToken)
            {
                var filter = request.Filter;

                var query = from p in _dataContext.VcPets.AsNoTracking()
                            join u in _dataContext.VcUsers.AsNoTracking() on p.OwnerId equals u.Id
                            where p.IsActive
                            select new PetModel
                            {
                                Id = p.Id,
                                Code = p.Code,
                                Name = p.Name,
                                Species = p.Species,
                                Breed = p.Breed,
                                Gender = p.Gender,
                                IsNeutered = p.IsNeutered,
                                BirthDate = p.BirthDate,
                                Weight = p.Weight,
                                Color = p.Color,
                                ImageUrl = p.ImageUrl,
                                OwnerId = p.OwnerId,
                                OwnerName = u.FullName,
                                Note = p.Note,
                                IsActive = p.IsActive,
                                Order = p.Order,
                                CreatedDate = p.CreatedDate
                            };

                // Filter by a specific OwnerId if provided
                if (filter.OwnerId.HasValue && filter.OwnerId > 0)
                {
                    query = query.Where(x => x.OwnerId == filter.OwnerId.Value);
                }

                // TextSearch on Pet's Name, Code, Species, or Breed
                if (!string.IsNullOrEmpty(filter.TextSearch))
                {
                    string ts = filter.TextSearch.Trim().ToLower();
                    query = query.Where(x =>
                        x.Name.ToLower().Contains(ts) ||
                        x.Code.ToLower().Contains(ts) ||
                        x.Species.ToLower().Contains(ts) ||
                        x.Breed.ToLower().Contains(ts));
                }
                
                if (filter.IsActive.HasValue)
                {
                    query = query.Where(x => x.IsActive == filter.IsActive.Value);
                }

                query = query.OrderByField(filter.PropertyName, filter.Ascending);

                if (filter.PageSize <= 0)
                {
                    filter.PageSize = 10;
                }

                int totalCount = await query.CountAsync(cancellationToken);

                int excludedRows = (filter.PageNumber - 1) * filter.PageSize;
                if (excludedRows < 0) excludedRows = 0;

                var listData = await query
                    .Skip(excludedRows)
                    .Take(filter.PageSize)
                    .ToListAsync(cancellationToken);

                return new PaginationList<PetModel>()
                {
                    DataCount = listData.Count,
                    TotalCount = totalCount,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize,
                    Data = listData
                };
            }
        }
    }
}
