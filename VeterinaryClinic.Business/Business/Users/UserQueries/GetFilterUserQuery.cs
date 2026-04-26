using MediatR;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VeterinaryClinic.Business
{
    public class GetFilterUserQuery : IRequest<PaginationList<UserModel>>
    {
        public UserFilterModel Filter {get; set;}

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

                var query = _dataContext.VcUsers.AsNoTracking();

                // Base filter for DOCTOR and RECEPTIONIST roles
                query = query.Where(x => x.Role == Role.DOCTOR.ToString() || x.Role == Role.RECEPTIONIST.ToString());

                // Lọc theo từ khóa chung (TextSearch)
                if (!string.IsNullOrEmpty(filter.TextSearch))
                {
                    string ts = filter.TextSearch.Trim().ToLower();
                    query = query.Where(x => 
                        x.FullName.ToLower().Contains(ts) || 
                        x.Code.ToLower().Contains(ts) ||
                        x.Username.ToLower().Contains(ts) ||
                        x.Email.ToLower().Contains(ts) ||
                        x.PhoneNumber.Contains(ts));
                }
                
                // Lọc theo IsActive
                if (filter.IsActive.HasValue)
                {
                    query = query.Where(x => x.IsActive == filter.IsActive.Value);
                }
                else 
                {
                    query = query.Where(x => x.IsActive);
                }

                // Lọc theo Role nếu được cung cấp
                if (!string.IsNullOrEmpty(filter.Role))
                {
                    string roleInput = filter.Role.Trim().ToUpper();
                    if (roleInput == Role.DOCTOR.ToString() || roleInput == Role.RECEPTIONIST.ToString())
                    {
                        query = query.Where(x => x.Role == roleInput);
                    }
                    else
                    {
                        // If an invalid role is passed, return no results as it's outside the allowed scope.
                        query = query.Where(x => false);
                    }
                }

                // Sắp xếp
                query = query.OrderByField(filter.PropertyName, filter.Ascending);

                // Phân trang
                if (filter.PageSize <= 0) filter.PageSize = 10;
                if (filter.PageNumber <= 0) filter.PageNumber = 1;

                int totalCount = await query.CountAsync(cancellationToken);
                
                int excludedRows = (filter.PageNumber - 1) * filter.PageSize;
                
                var listData = await query
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
