using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class GetUserByIdQuery : IRequest<UserModel>
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

        public class Handler : IRequestHandler<GetUserByIdQuery, UserModel>
        {
            private readonly VeterinaryClinicReadDataContext _dataContext;
            private readonly ICacheService _cacheService;

            public Handler(VeterinaryClinicReadDataContext dataContext, ICacheService cacheService)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
            }

            public async Task<UserModel> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
            {
                var id = request.Id;
                string cacheKey = UserConstant.BuildCacheKey(id.ToString());
                
                var item = await _cacheService.GetOrCreate<UserModel>(cacheKey, async () =>
                {
                    var entity = await _dataContext.VcUsers.AsNoTracking()
                        .Where(x => x.Id == id)
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

                    if (entity != null && entity.Role == Role.DOCTOR.ToString())
                    {
                        var specializationIds = await _dataContext.VcDoctorSpecializations
                            .Where(ds => ds.DoctorId == entity.Id)
                            .Select(ds => ds.SpecializationId)
                            .ToListAsync(cancellationToken);
                        
                        if (specializationIds.Any())
                        {
                            entity.Specializations = await _dataContext.VcSpecializations
                                .Where(s => specializationIds.Contains(s.Id) && s.IsActive)
                                .ToListAsync(cancellationToken);
                        }
                    }
                        
                    return entity;
                });
                
                return item;
            }
        }
    }
}
