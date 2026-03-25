using MediatR;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using System;

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

                // Lấy danh sách user đang active (nếu không có IsActive trong filter thì mặc định)
                var data = _dataContext.VcUsers
                        .AsNoTracking()
                        .AsQueryable();

                // Lọc theo từ khóa chung (TextSearch) - Tìm theo FullName, Code, Username, Email, PhoneNumber
                if (!string.IsNullOrEmpty(filter.TextSearch))
                {
                    string ts = filter.TextSearch.Trim().ToLower();
                    data = data.Where(x => 
                        x.FullName.ToLower().Contains(ts) || 
                        x.Code.ToLower().Contains(ts) ||
                        x.Username.ToLower().Contains(ts) ||
                        x.Email.ToLower().Contains(ts) ||
                        x.PhoneNumber.Contains(ts)); // Số điện thoại thường không có hoa/thường
                }
                
                // Lọc theo IsActive nếu được truyền
                if (filter.IsActive.HasValue)
                {
                    data = data.Where(x => x.IsActive == filter.IsActive.Value);
                }
                else 
                {
                    // Mặc định chỉ lấy user active nếu FE không gửi
                    data = data.Where(x => x.IsActive);
                }

                // Lọc theo Role
                if (!string.IsNullOrEmpty(filter.Role))
                {
                    string roleInput = filter.Role.Trim().ToUpper();
                    
                    // Kiểm tra xem Role truyền vào có nằm trong Enum Role không
                    if (!Enum.IsDefined(typeof(Role), roleInput))
                    {
                        throw new ArgumentException($"Role '{filter.Role}' is invalid.");
                    }
                    
                    data = data.Where(x => x.Role.ToUpper() == roleInput);
                }

                // Sắp xếp
                data = data.OrderByField(filter.PropertyName, filter.Ascending);

                // Phân trang an toàn
                if (filter.PageSize <= 0) filter.PageSize = 10;
                if (filter.PageNumber <= 0) filter.PageNumber = 1;

                // Tổng bản ghi
                int totalCount = await data.CountAsync(cancellationToken);
                
                // Tính số dòng bỏ qua
                int excludedRows = (filter.PageNumber - 1) * filter.PageSize;
                
                // Lấy dữ liệu phân trang và sử dụng Select để chỉ lấy các trường cần thiết (bảo mật)
                var listData = await data
                    .Skip(excludedRows)
                    .Take(filter.PageSize)
                    .Select(x => new UserModel 
                    {
                        Id = x.Id,
                        Code = x.Code,
                        UserName = x.Username,
                        Email = x.Email,
                        FullName = x.FullName,
                        PhoneNumber = x.PhoneNumber,
                        Gender = x.Gender,
                        Address = x.Address,
                        AvatarUrl = x.AvatarUrl,
                        Role = x.Role,
                        IsActive = x.IsActive,
                        CreatedDate = x.CreatedDate
                    })
                    .ToListAsync(cancellationToken);

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