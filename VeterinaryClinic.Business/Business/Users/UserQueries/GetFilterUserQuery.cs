using MediatR;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class GetFilterUserQuery : IRequest<PaginationList<UserModel>>
    {
        public UserFilterModel Filter {get; set;}

        /// <summary>
        /// Lay danh sach nguoi dung theo dieu kien loc
        /// </summary>
        /// <param name="filter">Thong tin loc</param>
        public GetFilterUserQuery(UserFilterModel filter)
        {
            Filter = filter;
        }

        public class Handler : IRequestHandler<GetFilterUserQuery, PaginationList<UserModel>>
        {
            private readonly VeterinaryClinicReadDataContext _dataContext;

            public Handler(VeterinaryClinicReadDataContext dataContext)
            {
                _dataContext = dataContext;
            }

            public async Task<PaginationList<UserModel>> Handle(GetFilterUserQuery request,
                CancellationToken cancellationToken)
            {
                var filter = request.Filter;

                // Lấy danh sách user đang active
                var data = (from dt in _dataContext.VcUsers
                        .AsNoTracking()
                        .Where(x => x.IsActive)
                    select dt);

                // Lọc theo từ khóa chung (TextSearch) - Tìm theo FullName, Code, Username
                if (!string.IsNullOrEmpty(filter.TextSearch))
                {
                    string ts = filter.TextSearch.Trim().ToLower();
                    data = data.Where(x => 
                        x.FullName.ToLower().Contains(ts) || 
                        x.Code.ToLower().Contains(ts) ||
                        x.Username.ToLower().Contains(ts));
                }
                
                if (filter.IsActive.HasValue)
                {
                    data = data.Where(x => x.IsActive == filter.IsActive.Value);
                }

                #region Các điều kiện lọc cụ thể của User

                if (!string.IsNullOrEmpty(filter.Code))
                {
                    string code = filter.Code.Trim().ToLower();
                    data = data.Where(x => x.Code.ToLower().Contains(code));
                }

                if (!string.IsNullOrEmpty(filter.FullName))
                {
                    string name = filter.FullName.Trim().ToLower();
                    data = data.Where(x => x.FullName.ToLower().Contains(name));
                }

                if (!string.IsNullOrEmpty(filter.Email))
                {
                    string email = filter.Email.Trim().ToLower();
                    data = data.Where(x => x.Email.ToLower().Contains(email));
                }

                if (!string.IsNullOrEmpty(filter.PhoneNumber))
                {
                    string phone = filter.PhoneNumber.Trim().ToLower();
                    data = data.Where(x => x.PhoneNumber.Contains(phone));
                }

                if (!string.IsNullOrEmpty(filter.Role))
                {
                    string role = filter.Role.Trim().ToLower();
                    data = data.Where(x => x.Role.ToLower() == role); // Role thường so sánh bằng chính xác
                }

                #endregion 
                

                // Sắp xếp
                data = data.OrderByField(filter.PropertyName, filter.Ascending);

                // Phân trang
                if (filter.PageSize <= 0)
                {
                    filter.PageSize = 10;
                }

                // Tổng bản ghi
                int totalCount = await data.CountAsync(cancellationToken);
                
                // Tính số dòng bỏ qua
                int excludedRows = (filter.PageNumber - 1) * filter.PageSize;
                if (excludedRows <= 0)
                {
                    excludedRows = 0;
                }
                
                // Lấy dữ liệu phân trang và sử dụng Select để chỉ lấy các trường cần thiết (bảo mật)
                var listData = await data
                    .Select(x => new UserModel 
                    {
                        Id = x.Id,
                        Code = x.Code,
                        UserName = x.Username,
                        Email = x.Email,
                        FullName = x.FullName,
                        PhoneNumber = x.PhoneNumber,
                        AvatarUrl = x.AvatarUrl,
                        Role = x.Role
                    })
                    .Skip(excludedRows)
                    .Take(filter.PageSize)
                    .ToListAsync(cancellationToken);

                // Không cần dùng AutoMapper nữa vì đã dùng .Select()
                
                return new PaginationList<UserModel>()
                {
                    DataCount = listData.Count,
                    TotalCount = totalCount,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize,
                    Data = listData
                };
            }
        }
    }   
}