using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class GetUserInfoLoggedInQuery : IRequest<UserModel>
    {
        public class Handler : IRequestHandler<GetUserInfoLoggedInQuery, UserModel>
        {
            private readonly VeterinaryClinicReadDataContext _dataContext;
            private readonly ICacheService _cacheService;
            private readonly IContextAccessor _contextAccessor;
            private readonly IMediator _mediator;

            public Handler(VeterinaryClinicReadDataContext dataContext, ICacheService cacheService, System.Func<IContextAccessor> contextAccessorFactory, IMediator mediator)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
                _contextAccessor = contextAccessorFactory();
                _mediator = mediator;
            }

            public async Task<UserModel> Handle(GetUserInfoLoggedInQuery request, CancellationToken cancellationToken)
            {
                var id = _contextAccessor.UserId;
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

                    if (entity != null)
                    {
                        switch (entity.Role)
                        {
                            case "DOCTOR":
                                var specializationIds = await _dataContext.VcDoctorSpecializations
                                    .Where(ds => ds.DoctorId == entity.Id)
                                    .Select(ds => ds.SpecializationId)
                                    .ToListAsync(cancellationToken);
                                
                                entity.Specializations = await _dataContext.VcSpecializations
                                    .Where(s => specializationIds.Contains(s.Id) && s.IsActive)
                                    .ToListAsync(cancellationToken);
                                break;
                            case "CUSTOMER":
                                var petFilterModel = new PetFilterModel
                                {
                                    OwnerId = entity.Id,
                                    IsActive = true,
                                    PageNumber = 1,
                                    PageSize = 100 // Assuming a customer won't have more than 100 pets
                                };
                                var petFilter = new GetFilterPetQuery(petFilterModel);
                                var petResult = await _mediator.Send(petFilter, cancellationToken);
                                entity.Pets = petResult.Data;
                                break;
                        }
                    }

                    return entity;
                });

                return item;
            }
        }
    }
}
