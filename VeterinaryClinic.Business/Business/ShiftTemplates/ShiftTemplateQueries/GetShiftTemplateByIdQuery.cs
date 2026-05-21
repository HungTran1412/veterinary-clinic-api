using MediatR;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class GetShiftTemplateByIdQuery : IRequest<ShiftTemplateModel>
    {
        public int Id { get; }

        public GetShiftTemplateByIdQuery(int id)
        {
            Id = id;
        }

        public class Handler : IRequestHandler<GetShiftTemplateByIdQuery, ShiftTemplateModel>
        {
            private readonly VeterinaryClinicReadDataContext _dataContext;
            private readonly ICacheService _cacheService;

            public Handler(VeterinaryClinicReadDataContext dataContext, ICacheService cacheService)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
            }

            public async Task<ShiftTemplateModel> Handle(GetShiftTemplateByIdQuery request, CancellationToken cancellationToken)
            {
                var id = request.Id;
                string cacheKey = ShiftTemplateConstant.BuildCacheKey(id.ToString());
                var item = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var entity = await _dataContext.VcShiftTemplates.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
                    return AutoMapperUtils.AutoMap<VcShiftTemplates, ShiftTemplateModel>(entity);
                });
                return item;
            }
        }
    }
}
