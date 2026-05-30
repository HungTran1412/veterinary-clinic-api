using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using System.Text.Json;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using VeterinaryClinic.Shared.ContextAccessor;

namespace VeterinaryClinic.Business
{
    public class UpdateMedicalRecordCommand : IRequest<Unit>
    {
        public int Id { get; }
        public UpdateMedicalRecordModel Model { get; }

        public UpdateMedicalRecordCommand(int id, UpdateMedicalRecordModel model)
        {
            Id = id;
            Model = model;
        }

        public class Handler : IRequestHandler<UpdateMedicalRecordCommand, Unit>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly ICacheService _cacheService;
            private readonly IContextAccessor _contextAccessor;
            private readonly IStringLocalizer<UpdateMedicalRecordCommand> _localizer;

            public Handler(VeterinaryClinicDataContext dataContext, ICacheService cacheService, Func<IContextAccessor> contextAccessorFactory, IStringLocalizer<UpdateMedicalRecordCommand> localizer)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
                _contextAccessor = contextAccessorFactory();
                _localizer = localizer;
            }

            public async Task<Unit> Handle(UpdateMedicalRecordCommand request, CancellationToken cancellationToken)
            {
                var model = request.Model;
                Log.Information($"Update Medical Record: {JsonSerializer.Serialize(model)}");

                //check phải đúng người khám mới được sửa
                if(_contextAccessor.UserId != model.DoctorId)
                {
                    throw new ArgumentException(_localizer["medical_record.no-permission"]);
                }
                    
                //check role người dùng
                if (_contextAccessor.Role != Role.DOCTOR.ToString())
                {
                    throw new ArgumentException(_localizer["medical_record.no-permission"]);
                }
                
                var entity = await _dataContext.VcMedicalRecords.FindAsync(request.Id);
                if (entity == null)
                {
                    throw new ArgumentException(_localizer["medical_record.not_found"]);
                }

                // Update entity properties from the model
                model.UpdateEntity(entity);
                entity.ModifiedUserId = _contextAccessor.UserId;
                entity.ModifiedDate = DateTime.UtcNow;
                entity.ModifiedUserName = _contextAccessor.UserName;


                _dataContext.VcMedicalRecords.Update(entity);
                await _dataContext.SaveChangesAsync(cancellationToken);

                // Assuming a constant exists for this, like in the reference command.
                _cacheService.Remove(MedicalRecordConstant.BuildCacheKey(entity.Id.ToString()));
                _cacheService.Remove(MedicalRecordConstant.BuildCacheKey());

                Log.Information($"Medical Record updated successfully with Id: {entity.Id}");

                return Unit.Value;
            }
        }
    }
}
