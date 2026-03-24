using MediatR;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class GetUserByIdQuery : IRequest<UserBaseModel>
    {
        public int Id { get; }

        /// <summary>
        /// Lay thong tin nguoi dung theo id
        /// </summary>
        /// <param name="id">id nguoi dung</param>
        public GetUserByIdQuery(int id)
        {
            Id = id;
        }

        public class Handler : IRequestHandler<GetUserByIdQuery, UserBaseModel>
        {
            private readonly VeterinaryClinicReadDataContext _dataContext;
            private readonly ICacheService _cacheService;

            public Handler(VeterinaryClinicReadDataContext dataContext, ICacheService cacheService)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
            }

            public async Task<UserBaseModel> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
            {
                var id = request.Id;
                string cacheKey = UserConstant.BuildCacheKey(id.ToString());
                
                var item = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var entity = await _dataContext.VcUsers.AsNoTracking()
                        .Where(x => x.Id == id)
                        // Sử dụng Projection để chỉ lấy các trường cần thiết và tránh lộ mật khẩu
                        .Select(x => new UserModel
                        {
                            Id = x.Id,
                            Code = x.Code,
                            UserName = x.Username,
                            Email = x.Email,
                            FullName = x.FullName,
                            PhoneNumber = x.PhoneNumber,
                            Gender = x.Gender,
                            AvatarUrl = x.AvatarUrl,
                            Role = x.Role
                        })
                        .FirstOrDefaultAsync(cancellationToken);
                        
                    return entity;
                });
                
                return item;
            }
        }
    }
}
