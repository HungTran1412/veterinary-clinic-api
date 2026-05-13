using MediatR;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business;

public class GetInvoiceByIdQuery : IRequest<InvoiceModel>
{
    public int Id { get; }

    /// <summary>
    /// Lay thong tin hoa don chi tiet
    /// </summary>
    /// <param name="id"></param>
    public GetInvoiceByIdQuery(int id)
    {
        Id = id;
    }

    public class Handler : IRequestHandler<GetInvoiceByIdQuery, InvoiceModel>
    {
        private readonly VeterinaryClinicReadDataContext _dataContext;
        private readonly ICacheService _cacheService;

        public Handler(VeterinaryClinicReadDataContext dataContext, ICacheService cacheService)
        {
            _dataContext = dataContext;
            _cacheService = cacheService;
        }

        public async Task<InvoiceModel> Handle(GetInvoiceByIdQuery request, CancellationToken cancellationToken)
        {
            var id = request.Id;
            string cacheKey = InvoiceConstant.BuildCacheKey(id.ToString());

            var item = await _cacheService.GetOrCreate(cacheKey, async () =>
            {
                var entity = await _dataContext.VcInvoices.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
                
                return AutoMapperUtils.AutoMap<VcInvoices, InvoiceModel>(entity);
            });
            
            return item;
        }
    }
}
