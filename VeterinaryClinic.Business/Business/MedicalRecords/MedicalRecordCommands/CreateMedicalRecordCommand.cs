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
    public class CreateMedicalRecordCommand : IRequest<int>
    {
        public CreateMedicalRecordModel Model { get; }

        public CreateMedicalRecordCommand(CreateMedicalRecordModel model)
        {
            Model = model;
        }

        public class Handler : IRequestHandler<CreateMedicalRecordCommand, int>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly ICacheService _cacheService;
            private readonly IContextAccessor _contextAccessor;
            private readonly IStringLocalizer<CreateMedicalRecordCommand> _localizer;

            public Handler(VeterinaryClinicDataContext dataContext, ICacheService cacheService, Func<IContextAccessor> contextAccessorFactory, IStringLocalizer<CreateMedicalRecordCommand> localizer)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
                _contextAccessor = contextAccessorFactory();
                _localizer = localizer;
            }

            public async Task<int> Handle(CreateMedicalRecordCommand request, CancellationToken cancellationToken)
            {
                var model = request.Model;
                Log.Information($"Create Medical Record: {JsonSerializer.Serialize(model)}");

                // 1. Validate Appointment exists
                var appointment = await _dataContext.VcAppointments.FindAsync(model.AppointmentId);
                if (appointment == null)
                {
                    throw new ArgumentException(_localizer["appointment.not_found"]);
                }

                // 2. Validate Doctor exists
                var doctor = await _dataContext.VcUsers.FirstOrDefaultAsync(u => u.Id == model.DoctorId && u.Role == Role.DOCTOR.ToString(), cancellationToken);
                if (doctor == null)
                {
                    throw new ArgumentException(_localizer["doctor.not_found"]);
                }
                
                // 3. Check if a medical record for this appointment already exists
                var existingRecord = await _dataContext.VcMedicalRecords.AnyAsync(mr => mr.AppointmentId == model.AppointmentId, cancellationToken);
                if (existingRecord)
                {
                    throw new ArgumentException(_localizer["medical_record.already_exists_for_appointment"]);
                }

                var entity = new VcMedicalRecords
                {
                    Code = GenerateCodeUtils.GenerateCode("MR"),
                    AppointmentId = model.AppointmentId,
                    DoctorId = model.DoctorId,
                    Symptoms = model.Symptoms,
                    Diagnosis = model.Diagnosis,
                    TreatmentPlan = model.TreatmentPlan,
                    Prescription = model.Prescription,
                    DoctorNote = model.DoctorNote,
                    CompletedDate = model.CompletedDate,
                };

                await _dataContext.VcMedicalRecords.AddAsync(entity, cancellationToken);
                await _dataContext.SaveChangesAsync(cancellationToken);

               //xoa cache
                _cacheService.Remove(MedicalRecordConstant.BuildCacheKey());

                Log.Information($"Medical Record created successfully with Id: {entity.Id}");

                return entity.Id;
            }
        }
    }
}
