using MediatR;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class GetFilterServiceQuery : IRequest<PaginationList<ServiceBaseModel>>
    {
        public ServiceFilterModel Filter {get; set;}

        /// <summary>
        /// Lay danh sach dich vu theo dieu kien loc
        /// </summary>
        /// <param name="filter">Thong tin loc</param>
        public GetFilterServiceQuery(ServiceFilterModel filter)
        {
            Filter = filter;
        }

        public class Handler : IRequestHandler<GetFilterServiceQuery, PaginationList<ServiceBaseModel>>
        {
            private readonly VeterinaryClinicReadDataContext _dataContext;

            public Handler(VeterinaryClinicReadDataContext dataContext)
            {
                _dataContext = dataContext;
            }

            public async Task<PaginationList<ServiceBaseModel>> Handle(GetFilterServiceQuery request,
                CancellationToken cancellationToken)
            {
                var filter = request.Filter;

                var data = (from dt in _dataContext.VcServices
                        .AsNoTracking()
                        .Where(x => x.IsActive)
                    select dt);

                if (!string.IsNullOrEmpty(filter.TextSearch))
                {
                    string ts = filter.TextSearch.Trim().ToLower();
                    data = data.Where(x => x.Name.ToLower().Contains(ts) || x.Code.ToLower().Contains(ts));
                }

                if (filter.IsActive.HasValue)
                {
                    data = data.Where(x => x.IsActive == filter.IsActive.Value);
                }
                
                if (filter.SpecializationId.HasValue)
                {
                    data = data.Where(x => x.SpecializationId == filter.SpecializationId.Value);
                }
                
                if (filter.IsAvailable.HasValue)
                {
                    data = data.Where(x => x.IsAvailable == filter.IsAvailable.Value);
                }

                data = data.OrderByField(filter.PropertyName, filter.Ascending);

                if (filter.PageSize <= 0)
                {
                    filter.PageSize = 10;
                }

                //tong ban ghi
                int totalCount = await data.CountAsync(cancellationToken);
                
                //tinh so dong bi bo qua trong kich thuoc trang
                int excludedRows = (filter.PageNumber - 1) * filter.PageSize;
                if (excludedRows <= 0)
                {
                    excludedRows = 0;
                }
                
                // ap dung phan trang
                var listData = await data
                    .Skip(excludedRows)
                    .Take(filter.PageSize)
                    .ToListAsync(cancellationToken);

                var listResult = AutoMapperUtils.AutoMap<VcServices, ServiceBaseModel>(listData);
                
                return new PaginationList<ServiceBaseModel>()
                {
                    DataCount = listResult.Count,
                    TotalCount = totalCount,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize,
                    Data = listResult
                };
            }
        }
    }   
}