using MediatR;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class GetFilterMedicalRecordQuery : IRequest<PaginationList<MedicalRecordModel>>
    {
        public MedicalRecordFilterModel Filter { get; }

        public GetFilterMedicalRecordQuery(MedicalRecordFilterModel filter)
        {
            Filter = filter;
        }

        public class Handler : IRequestHandler<GetFilterMedicalRecordQuery, PaginationList<MedicalRecordModel>>
        {
            private readonly VeterinaryClinicReadDataContext _dataContext;

            public Handler(VeterinaryClinicReadDataContext dataContext)
            {
                _dataContext = dataContext;
            }

            public async Task<PaginationList<MedicalRecordModel>> Handle(GetFilterMedicalRecordQuery request, CancellationToken cancellationToken)
            {
                var filter = request.Filter;

                var query = _dataContext.VcMedicalRecords
                    .AsNoTracking()
                    .Where(mr => mr.IsActive);

                if (!string.IsNullOrEmpty(filter.TextSearch))
                {
                    string ts = filter.TextSearch.Trim().ToLower();
                    query = query.Where(mr => mr.Code.ToLower().Contains(ts) ||
                                              mr.Symptoms.ToLower().Contains(ts) ||
                                              mr.Diagnosis.ToLower().Contains(ts));
                }

                var totalCount = await query.CountAsync(cancellationToken);

                var pagedQuery = query.OrderByField(filter.PropertyName, filter.Ascending)
                                      .Skip((filter.PageNumber - 1) * filter.PageSize)
                                      .Take(filter.PageSize);

                var medicalRecords = await pagedQuery.ToListAsync(cancellationToken);

                var mappedMedicalRecords = AutoMapperUtils.AutoMap<VcMedicalRecords, MedicalRecordModel>(medicalRecords);

                return new PaginationList<MedicalRecordModel>
                {
                    Data = mappedMedicalRecords,
                    TotalCount = totalCount,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize
                };
            }
        }
    }
}
