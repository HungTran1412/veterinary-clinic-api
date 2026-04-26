using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class CreateDoctorSpecializationCommand : IRequest<Unit>
    {
        public DoctorSpecializationModel Model { get; }

        public CreateDoctorSpecializationCommand(DoctorSpecializationModel model)
        {
            Model = model;
        }

        public class Handler : IRequestHandler<CreateDoctorSpecializationCommand, Unit>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly IStringLocalizer<CreateDoctorSpecializationCommand> _localizer;

            public Handler(VeterinaryClinicDataContext dataContext, IStringLocalizer<CreateDoctorSpecializationCommand> localizer)
            {
                _dataContext = dataContext;
                _localizer = localizer;
            }

            public async Task<Unit> Handle(CreateDoctorSpecializationCommand request, CancellationToken cancellationToken)
            {
                var model = request.Model;

                // Validate Doctor
                var doctor = await _dataContext.VcUsers.FirstOrDefaultAsync(u => u.Id == model.DoctorId && u.IsActive, cancellationToken);
                if (doctor == null)
                {
                    throw new ArgumentException(_localizer["doctor.not_found"]);
                }
                if (doctor.Role != Role.DOCTOR.ToString())
                {
                    throw new ArgumentException(_localizer["user.invalid.role_must_be_doctor"]);
                }

                // Validate Specializations
                var specializations = await _dataContext.VcSpecializations
                    .Where(s => model.SpecializationIds.Contains(s.Id) && s.IsActive)
                    .ToListAsync(cancellationToken);

                if (specializations.Count != model.SpecializationIds.Count)
                {
                    throw new ArgumentException(_localizer["specialization.not_found_or_inactive"]);
                }

                // Check for existing specializations
                var existingLinks = await _dataContext.VcDoctorSpecializations
                    .Where(ds => ds.DoctorId == model.DoctorId && model.SpecializationIds.Contains(ds.SpecializationId))
                    .ToListAsync(cancellationToken);

                if (existingLinks.Any())
                {
                    var existingSpecNames = specializations.Where(s => existingLinks.Any(l => l.SpecializationId == s.Id)).Select(s => s.Name);
                    throw new ArgumentException($"{_localizer["doctor_specialization.already_exists"]} {string.Join(", ", existingSpecNames)}");
                }

                // Create new links
                var newDoctorSpecializations = model.SpecializationIds.Select(specId => new VcDoctorSpecializations
                {
                    DoctorId = model.DoctorId,
                    SpecializationId = specId
                });

                await _dataContext.VcDoctorSpecializations.AddRangeAsync(newDoctorSpecializations, cancellationToken);
                await _dataContext.SaveChangesAsync(cancellationToken);

                return Unit.Value;
            }
        }
    }
}
