using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class GetServiceByIdQuery : IRequest<InfoServiceModel>
    {
        public int Id { get; }

        public GetServiceByIdQuery(int id)
        {
            Id = id;
        }

        public class Handler : IRequestHandler<GetServiceByIdQuery, InfoServiceModel>
        {
            private readonly VeterinaryClinicReadDataContext _dataContext;
            private readonly ICacheService _cacheService;

            public Handler(VeterinaryClinicReadDataContext dataContext, ICacheService cacheService)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
            }

            public async Task<InfoServiceModel> Handle(GetServiceByIdQuery request, CancellationToken cancellationToken)
            {
                var id = request.Id;
                string cacheKey = ServiceConstant.BuildCacheKey(id.ToString());

                var item = await _cacheService.GetOrCreate<InfoServiceModel>(cacheKey, async () =>
                {
                    var query = from service in _dataContext.VcServices.AsNoTracking()
                                join specialization in _dataContext.VcSpecializations.AsNoTracking()
                                on service.SpecializationId equals specialization.Id
                                where service.Id == id
                                select new InfoServiceModel
                                {
                                    Id = service.Id,
                                    Code = service.Code,
                                    Name = service.Name,
                                    Price = service.Price,
                                    DurationMinutes = service.DurationMinutes,
                                    SpecializationId = service.SpecializationId,
                                    SpecializationName = specialization.Name,
                                    ImageUrl = service.ImageUrl,
                                    IsAvailable = service.IsAvailable,
                                    Description = service.Description,
                                    IsActive = service.IsActive,
                                    Order = service.Order,
                                    CreatedDate = service.CreatedDate
                                };

                    return await query.FirstOrDefaultAsync(cancellationToken);
                });

                return item;
            }
        }
    }
}
