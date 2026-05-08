using MediatR;
using Microsoft.Extensions.Localization;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class GetMedicalRecordByIdQuery : IRequest<MedicalInfoModel>
    {
        public int Id { get; }

        public GetMedicalRecordByIdQuery(int id)
        {
            Id = id;
        }

        public class Handler : IRequestHandler<GetMedicalRecordByIdQuery, MedicalInfoModel>
        {
            private readonly IStringLocalizer<GetMedicalRecordByIdQuery> _localizer;
            private readonly IVeterinaryClinicCallStoreHelper _callStoreHelper;

            public Handler(IStringLocalizer<GetMedicalRecordByIdQuery> localizer, IVeterinaryClinicCallStoreHelper callStoreHelper)
            {
                _localizer = localizer;
                _callStoreHelper = callStoreHelper;
            }

            public async Task<MedicalInfoModel> Handle(GetMedicalRecordByIdQuery request, CancellationToken cancellationToken)
            {
                var dataTable = _callStoreHelper.CallStoreGetMedicalRecordByIdAsync(request.Id);

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    throw new KeyNotFoundException(_localizer["medical_record.not_found"]);
                }

                var medicalInfo = dataTable.Rows[0].ToObject<MedicalInfoModel>();
                
                return await Task.FromResult(medicalInfo);
            }
        }
    }
}
