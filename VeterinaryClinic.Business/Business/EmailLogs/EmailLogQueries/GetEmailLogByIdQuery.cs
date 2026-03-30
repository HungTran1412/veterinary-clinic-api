using MediatR;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Business.Business.EmailLogs;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class GetEmailLogByIdQuery : IRequest<EmailLogModel>
    {
        public int Id { get; }

        /// <summary>
        /// Lay thong tin email da gui
        /// </summary>
        /// <param name="id"></param>
        public GetEmailLogByIdQuery(int id)
        {
            Id = id;
        }

        public class Handler : IRequestHandler<GetEmailLogByIdQuery, EmailLogModel>
        {
            private readonly VeterinaryClinicReadDataContext _dataContext;
            private readonly ICacheService _cacheService;

            public Handler(VeterinaryClinicReadDataContext dataContext, ICacheService cacheService)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
            }

            public async Task<EmailLogModel> Handle(GetEmailLogByIdQuery request, CancellationToken cancellationToken)
            {
                var id = request.Id;
                string cacheKey = EmailLogConstant.BuildCacheKey(id.ToString());
                var item = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var entity = await _dataContext.VcEmailLogs.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == id);

                    return AutoMapperUtils.AutoMap<VcEmailLogs, EmailLogModel>(entity);
                });
                return item;
            }
        }
    }
}
