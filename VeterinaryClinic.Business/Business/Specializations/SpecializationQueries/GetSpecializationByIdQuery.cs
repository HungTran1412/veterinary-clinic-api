using MediatR;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class GetSpecializationByIdQuery : IRequest<SpecializationModel>
    {
        public int Id { get; }

        /// <summary>
        /// Lấy thông tin chuyen nganh theo id
        /// </summary>
        /// <param name="id">id chuyen nganh</param>
        public GetSpecializationByIdQuery(int id)
        {
            Id = id;
        }

        public class Handler : IRequestHandler<GetSpecializationByIdQuery, SpecializationModel>
        {
            private readonly VeterinaryClinicReadDataContext _dataContext;
            private readonly ICacheService _cacheService;

            public Handler(VeterinaryClinicReadDataContext dataContext, ICacheService cacheService)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
            }

            public async Task<SpecializationModel> Handle(GetSpecializationByIdQuery request, CancellationToken cancellationToken)
            {
                var id = request.Id;
                string cacheKey = SpecializationConstant.BuildCacheKey(id.ToString());
                var item = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var entity = await _dataContext.VcSpecializations.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == id);
                    return AutoMapperUtils.AutoMap<VcSpecializations, SpecializationModel>(entity);
                });
                return item;
            }
        }
    }
}
