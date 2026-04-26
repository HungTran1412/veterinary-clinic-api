using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class UpdateUserCommand : IRequest<Unit>
    {
        public UpdateUserModel Model { get; }

        public UpdateUserCommand(UpdateUserModel model)
        {
            Model = model;
        }

        public class Handler : IRequestHandler<UpdateUserCommand, Unit>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly ICacheService _cacheService;
            private readonly IStringLocalizer<UpdateUserCommand> _localizer;
            private readonly IContextAccessor _contextAccessor;

            public Handler(VeterinaryClinicDataContext dataContext, ICacheService cacheService, IStringLocalizer<UpdateUserCommand> localizer, Func<IContextAccessor> contextAccessorFactory)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
                _localizer = localizer;
                _contextAccessor = contextAccessorFactory();
            }

            public async Task<Unit> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
            {
                var model = request.Model;
                Log.Information($"Update User: " + JsonSerializer.Serialize(model));

                var entity = await _dataContext.VcUsers.FirstOrDefaultAsync(x => x.Id == model.Id, cancellationToken);
                if (entity == null)
                {
                    throw new ArgumentException($"{_localizer["data.not-found"]}");
                }

                #region Validate
                //validate mail
                if (!ValidationUtils.IsValidEmail(model.Email))
                {
                    throw new ArgumentException($"{_localizer["user.invalid.email_format"]}");
                }

                //Can update only DOCTOR and RECEPTIONIST
                var upperRole = model.Role.Trim().ToUpper();
                if (upperRole != Role.DOCTOR.ToString() && upperRole != Role.RECEPTIONIST.ToString())
                {
                    throw new ArgumentException($"Admin can only update users with roles: {Role.DOCTOR}, {Role.RECEPTIONIST}");
                }
                
                // Validate that specializations are only provided for doctors
                if (upperRole != Role.DOCTOR.ToString() && model.SpecializationIds != null && model.SpecializationIds.Any())
                {
                    throw new ArgumentException(_localizer["user.specialization.not_for_receptionist"]);
                }

                // Kiểm tra trùng Email (loại trừ chính nó)
                var isEmailExisted = await _dataContext.VcUsers.AnyAsync(x => x.Email == model.Email && x.Id != model.Id, cancellationToken);
                if (isEmailExisted)
                {
                    throw new ArgumentException($"{_localizer["user.existed.email"]}");
                }

                // Kiểm tra trùng số điện thoại (loại trừ chính nó)
                var isPhoneExisted = await _dataContext.VcUsers.AnyAsync(x => x.PhoneNumber == model.PhoneNumber && x.Id != model.Id, cancellationToken);
                if (isPhoneExisted)
                {
                    throw new ArgumentException($"{_localizer["user.existed.phone_number"]}");
                }
                #endregion

                //cap nhat thong tin
                entity.ModifiedUserId = _contextAccessor.UserId;
                model.UpdateEntity(entity);

                // If the user is a DOCTOR, update their specializations.
                if (entity.Role == Role.DOCTOR.ToString())
                {
                    var existingSpecializations = _dataContext.VcDoctorSpecializations.Where(ds => ds.DoctorId == entity.Id);
                    _dataContext.VcDoctorSpecializations.RemoveRange(existingSpecializations);

                    if (model.SpecializationIds != null && model.SpecializationIds.Any())
                    {
                        var validSpecializations = await _dataContext.VcSpecializations
                            .Where(s => model.SpecializationIds.Contains(s.Id) && s.IsActive)
                            .Select(s => s.Id)
                            .ToListAsync(cancellationToken);

                        var newSpecializations = validSpecializations.Select(specId => new VcDoctorSpecializations
                        {
                            DoctorId = entity.Id,
                            SpecializationId = specId
                        });
                        await _dataContext.VcDoctorSpecializations.AddRangeAsync(newSpecializations, cancellationToken);
                    }
                }

                _dataContext.VcUsers.Update(entity);
                await _dataContext.SaveChangesAsync(cancellationToken);

                //xoa cache
                _cacheService.Remove(UserConstant.BuildCacheKey(entity.Id.ToString()));
                _cacheService.Remove(UserConstant.BuildCacheKey());

                return Unit.Value;
            }
        }
    }
}
