using MediatR;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class GetServiceByIdQuery : IRequest<ServiceModel>
    {
        public int Id { get; }

        /// <summary>
        /// Lay thong tin dich vu theo id
        /// </summary>
        /// <param name="id">id dich vu</param>
        public GetServiceByIdQuery(int id)
        {
            Id = id;
        }

        public class Handler : IRequestHandler<GetServiceByIdQuery, ServiceModel>
        {
            private readonly VeterinaryClinicReadDataContext _dataContext;
            private readonly ICacheService _cacheService;

            public Handler(VeterinaryClinicReadDataContext dataContext, ICacheService cacheService)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
            }

            public async Task<ServiceModel> Handle(GetServiceByIdQuery request, CancellationToken cancellationToken)
            {
                var id = request.Id;
                string cacheKey = ServiceConstant.BuildCacheKey(id.ToString());
                var item = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var entity = await _dataContext.VcServices.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == id);
                    return AutoMapperUtils.AutoMap<VcServices, ServiceModel>(entity);
                });
                return item;
            }
        }
    }
}
