using MediatR;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class GetFilterShiftTemplateQuery : IRequest<PaginationList<ShiftTemplateModel>>
    {
        public ShiftTemplateFilterModel Filter {get; set;}

        public GetFilterShiftTemplateQuery(ShiftTemplateFilterModel filter)
        {
            Filter = filter;
        }

        public class Handler : IRequestHandler<GetFilterShiftTemplateQuery, PaginationList<ShiftTemplateModel>>
        {
            private readonly VeterinaryClinicReadDataContext _dataContext;

            public Handler(VeterinaryClinicReadDataContext dataContext)
            {
                _dataContext = dataContext;
            }
            
            public async Task<PaginationList<ShiftTemplateModel>> Handle(GetFilterShiftTemplateQuery request,
                CancellationToken cancellationToken)
            {
                var filter = request.Filter;

                var data = (from dt in _dataContext.VcShiftTemplates
                        .AsNoTracking()
                        .Where(x => x.IsActive)
                    select dt);

                if (!string.IsNullOrEmpty(filter.TextSearch))
                {
                    string ts = filter.TextSearch.Trim().ToLower();
                    data = data.Where(x => x.ShiftName.ToLower().Contains(ts) || x.Code.ToLower().Contains(ts));
                }

                if (filter.IsActive.HasValue)
                {
                    data = data.Where(x => x.IsActive == filter.IsActive.Value);
                }

                data = data.OrderByField(filter.PropertyName, filter.Ascending);

                if (filter.PageSize <= 0)
                {
                    filter.PageSize = 10;
                }

                int totalCount = await data.CountAsync(cancellationToken);
                
                int excludedRows = (filter.PageNumber - 1) * filter.PageSize;
                if (excludedRows <= 0)
                {
                    excludedRows = 0;
                }
                
                var listData = await data
                    .Skip(excludedRows)
                    .Take(filter.PageSize)
                    .ToListAsync(cancellationToken);

                var listResult = AutoMapperUtils.AutoMap<VcShiftTemplates, ShiftTemplateModel>(listData);
                
                return new PaginationList<ShiftTemplateModel>()
                {
                    DataCount = listResult.Count,
                    TotalCount = totalCount,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize,
                    Data = listResult
                };
            }
        }
    }
}