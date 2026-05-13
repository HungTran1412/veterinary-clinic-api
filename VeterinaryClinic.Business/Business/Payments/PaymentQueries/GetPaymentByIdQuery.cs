using MediatR;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business;

public class GetPaymentByIdQuery : IRequest<PaymentModel>
{
    public int Id { get; }

    /// <summary>
    /// Lay thong tin thanh toan chi tiet
    /// </summary>
    /// <param name="id"></param>
    public GetPaymentByIdQuery(int id)
    {
        Id = id;
    }

    public class Handler : IRequestHandler<GetPaymentByIdQuery, PaymentModel>
    {
        private readonly VeterinaryClinicReadDataContext _dataContext;
        private readonly ICacheService _cacheService;

        public Handler(VeterinaryClinicReadDataContext dataContext, ICacheService cacheService)
        {
            _dataContext = dataContext;
            _cacheService = cacheService;
        }

        public async Task<PaymentModel> Handle(GetPaymentByIdQuery request, CancellationToken cancellationToken)
        {
            var id = request.Id;
            string cacheKey = PaymentConstant.BuildCacheKey(id.ToString());

            var item = await _cacheService.GetOrCreate(cacheKey, async () =>
            {
                var entity = await _dataContext.VcPayments.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id);
                
                return AutoMapperUtils.AutoMap<VcPayments, PaymentModel>(entity);
            });
            return item;
        }
    }
}