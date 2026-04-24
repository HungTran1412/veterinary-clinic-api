using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Serilog;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class UpdateUserLoggedInCommand : IRequest<Unit>
    {
        public UpdateUserLoggedInModel Model { get; set; }

        public class Handler : IRequestHandler<UpdateUserLoggedInCommand, Unit>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly IContextAccessor _contextAccessor;
            private readonly ICacheService _cacheService;
            private readonly IStringLocalizer<UpdateUserLoggedInCommand> _localizer;

            public Handler(VeterinaryClinicDataContext dataContext, ICacheService cacheService, Func<IContextAccessor> contextAccessorFactory)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
                _contextAccessor = contextAccessorFactory();
            }

            public async Task<Unit> Handle(UpdateUserLoggedInCommand request, CancellationToken cancellationToken)
            {
                var userId = _contextAccessor.UserId;
                var user = await _dataContext.VcUsers.FindAsync(userId);

                Log.Information($"Update User: " + JsonSerializer.Serialize(userId));
                if (user == null)
                {
                    throw new ArgumentException("User not found.");
                }

                // Update basic user information
                user.FullName = request.Model.FullName;
                user.PhoneNumber = request.Model.PhoneNumber;
                user.Gender = (int)request.Model.Gender;
                user.AvatarUrl = request.Model.AvatarUrl;

                // If the user is a doctor, update their specializations
                if (user.Role == Role.DOCTOR.ToString())
                {
                    // Remove existing specializations
                    var existingSpecializations = _dataContext.VcDoctorSpecializations.Where(ds => ds.DoctorId == user.Id);
                    _dataContext.VcDoctorSpecializations.RemoveRange(existingSpecializations);

                    // Add new specializations
                    if (request.Model.SpecializationIds != null && request.Model.SpecializationIds.Any())
                    {
                        var newSpecializations = request.Model.SpecializationIds.Select(specId => new VcDoctorSpecializations
                        {
                            DoctorId = user.Id,
                            SpecializationId = specId
                        });
                        await _dataContext.VcDoctorSpecializations.AddRangeAsync(newSpecializations, cancellationToken);
                    }
                }

                await _dataContext.SaveChangesAsync(cancellationToken);

                //xoa cache
                _cacheService.Remove(UserConstant.BuildCacheKey(user.Id.ToString()));
                _cacheService.Remove(UserConstant.BuildCacheKey());

                return Unit.Value;
            }
        }
    }
}
