using MediatR;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business;

public class GetUserInfoLoggedInQuery : IRequest<UserModel>
{
    public class Handler : IRequestHandler<GetUserInfoLoggedInQuery, UserModel>
    {
        private readonly VeterinaryClinicReadDataContext _dataContext;
        private readonly ICacheService _cacheService;
        private readonly IContextAccessor _contextAccessor;

        public Handler(VeterinaryClinicReadDataContext dataContext, ICacheService cacheService, Func<IContextAccessor> contextAccessorFactory)
        {
            _dataContext = dataContext;
            _cacheService = cacheService;
            _contextAccessor = contextAccessorFactory();
        }

        public async Task<UserModel> Handle(GetUserInfoLoggedInQuery request, CancellationToken cancellationToken)
        {
            var id = _contextAccessor.UserId;
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
