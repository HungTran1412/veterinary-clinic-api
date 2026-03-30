using MediatR;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class GetFilterSpecializationQuery : IRequest<PaginationList<SpecializationBaseModel>>
    {
        public SpecializationFilterModel Filter {get; set;}

        /// <summary>
        /// Lay danh sach chuyen nganh theo dieu kien loc
        /// </summary>
        /// <param name="filter">Thong tin loc</param>
        public GetFilterSpecializationQuery(SpecializationFilterModel filter)
        {
            Filter = filter;
        }

        public class Handler : IRequestHandler<GetFilterSpecializationQuery, PaginationList<SpecializationBaseModel>>
        {
            private readonly VeterinaryClinicReadDataContext _dataContext;

            public Handler(VeterinaryClinicReadDataContext dataContext)
            {
                _dataContext = dataContext;
            }
            
            public async Task<PaginationList<SpecializationBaseModel>> Handle(GetFilterSpecializationQuery request,
                CancellationToken cancellationToken)
            {
                var filter = request.Filter;

                var data = (from dt in _dataContext.VcSpecializations
                        .AsNoTracking()
                        .Where(x => x.IsActive)
                    select dt);

                if (!string.IsNullOrEmpty(filter.TextSearch))
                {
                    string ts = filter.TextSearch.Trim().ToLower();
                    data = data.Where(x => x.Name.ToLower().Contains(ts));
                }

                if (filter.IsActive.HasValue)
                {
                    data = data.Where(x => x.IsActive == filter.IsActive.Value);
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

                // Áp dụng lại AutoMap cho từng đối tượng
                var listResult = AutoMapperUtils.AutoMap<VcSpecializations, SpecializationBaseModel>(listData);
                
                // Đổi kiểu trả về thành BaseModel
                return new PaginationList<SpecializationBaseModel>()
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