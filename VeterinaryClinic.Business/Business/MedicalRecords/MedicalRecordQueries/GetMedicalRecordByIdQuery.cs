using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class GetMedicalRecordByIdQuery : IRequest<MedicalRecordModel>
    {
        public int Id { get; }

        public GetMedicalRecordByIdQuery(int id)
        {
            Id = id;
        }

        public class Handler : IRequestHandler<GetMedicalRecordByIdQuery, MedicalRecordModel>
        {
            private readonly VeterinaryClinicReadDataContext _dataContext;
            private readonly IStringLocalizer<GetMedicalRecordByIdQuery> _localizer;

            public Handler(VeterinaryClinicReadDataContext dataContext, IStringLocalizer<GetMedicalRecordByIdQuery> localizer)
            {
                _dataContext = dataContext;
                _localizer = localizer;
            }

            public async Task<MedicalRecordModel> Handle(GetMedicalRecordByIdQuery request, CancellationToken cancellationToken)
            {
                var medicalRecord = await _dataContext.VcMedicalRecords
                    .AsNoTracking()
                    .FirstOrDefaultAsync(mr => mr.Id == request.Id, cancellationToken);

                if (medicalRecord == null)
                {
                    throw new KeyNotFoundException(_localizer["medical_record.not_found"]);
                }

                return AutoMapperUtils.AutoMap<VcMedicalRecords, MedicalRecordModel>(medicalRecord);
            }
        }
    }
}
