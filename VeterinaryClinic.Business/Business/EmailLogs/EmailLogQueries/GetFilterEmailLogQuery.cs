using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class GetFilterEmailLogQuery : IRequest<PaginationList<EmailLogModel>>
    {
        public EmailLogFilterModel Filter { set; get; }

        /// <summary>
        /// Lấy danh sách email đã gửi
        /// </summary>
        /// <param name="filter"></param>
        public GetFilterEmailLogQuery(EmailLogFilterModel filter)
        {
            Filter = filter;
        }

        public class Handler : IRequestHandler<GetFilterEmailLogQuery, PaginationList<EmailLogModel>>
        {
            private readonly VeterinaryClinicReadDataContext _dataContext;
            
            public Handler(VeterinaryClinicReadDataContext dataContext)
            {
                _dataContext = dataContext;
            }

            public async Task<PaginationList<EmailLogModel>> Handle(GetFilterEmailLogQuery request,
                CancellationToken cancellationToken)
            {
                var filter = request.Filter;
                
                //lay du lieu
                var data = (from dt in _dataContext.VcEmailLogs
                    .AsNoTracking()
                    .Where(x=>x.IsActive)
                        select dt);
                
                //Dieu kien loc
                if (!string.IsNullOrEmpty(filter.TextSearch))
                {
                    string ts = filter.TextSearch.Trim().ToLower();
                    data = data.Where(x =>x.Code.ToLower().Contains(ts) || x.Subject.ToLower().Contains(ts) || x.ToEmail.ToLower().Contains(ts));
                }

                if (filter.IsActive.HasValue)
                {
                    data = data.Where(x => x.IsActive == filter.IsActive.Value);
                }

                // Sắp xếp theo ngày tạo mới nhất đến cũ nhất
                data = data.OrderByDescending(x => x.CreatedDate);
                
                if (filter.PageSize <= 0)
                {
                    filter.PageSize = 10;
                }

                //tong ban ghi
                int totalCount = await data.CountAsync(cancellationToken);
                
                //tinh so dong bi bo qua trong kich thuoc trang
                int excludedRows = (filter.PageNumber - 1) * filter.PageSize;
                if (excludedRows < 0)
                {
                    excludedRows = 0;
                }
                
                //phan trang
                var listData = await data
                    .Skip(excludedRows)
                    .Take(filter.PageSize)
                    .ToListAsync(cancellationToken);
                
                //map cho tung doi tuong
                var listResult = AutoMapperUtils.AutoMap<VcEmailLogs, EmailLogModel>(listData);

                return new PaginationList<EmailLogModel>()
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
