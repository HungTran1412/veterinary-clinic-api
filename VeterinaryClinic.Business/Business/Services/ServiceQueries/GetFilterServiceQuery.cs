using MediatR;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class GetFilterServiceQuery : IRequest<PaginationList<InfoServiceModel>>
    {
        public ServiceFilterModel Filter { get; set; }

        /// <summary>
        /// Lay danh sach dich vu theo dieu kien loc
        /// </summary>
        /// <param name="filter">Thong tin loc</param>
        public GetFilterServiceQuery(ServiceFilterModel filter)
        {
            Filter = filter;
        }

        public class Handler : IRequestHandler<GetFilterServiceQuery, PaginationList<InfoServiceModel>>
        {
            private readonly VeterinaryClinicReadDataContext _dataContext;

            public Handler(VeterinaryClinicReadDataContext dataContext)
            {
                _dataContext = dataContext;
            }

            public async Task<PaginationList<InfoServiceModel>> Handle(GetFilterServiceQuery request,
                CancellationToken cancellationToken)
            {
                var filter = request.Filter;

                var data = from s in _dataContext.VcServices.AsNoTracking()
                    join sp in _dataContext.VcSpecializations.AsNoTracking()
                        on s.SpecializationId equals sp.Id into spGroup
                    from sp in spGroup.DefaultIfEmpty()
                    where s.IsActive
                    select new InfoServiceModel
                    {
                        Id = s.Id,
                        Code = s.Code,
                        Name = s.Name,
                        Price = s.Price,
                        DurationMinutes = s.DurationMinutes,
                        SpecializationId = s.SpecializationId,
                        SpecializationName = sp.Name,
                        ImageUrl = s.ImageUrl,
                        IsAvailable = s.IsAvailable,
                        Description = s.Description,
                        IsActive = s.IsActive,
                        Order = s.Order,
                        CreatedDate = s.CreatedDate
                    };

                if (!string.IsNullOrEmpty(filter.TextSearch))
                {
                    string ts = filter.TextSearch.Trim().ToLower();
                    data = data.Where(x =>
                        x.Name.ToLower().Contains(ts) ||
                        x.Code.ToLower().Contains(ts));
                }

                #region Filter

                if (filter.IsActive.HasValue)
                {
                    data = data.Where(x => x.IsActive == filter.IsActive.Value);
                }

                if (filter.SpecializationId.HasValue && filter.SpecializationId > 0)
                {
                    data = data.Where(x => x.SpecializationId == filter.SpecializationId.Value);
                }

                if (filter.IsAvailable.HasValue)
                {
                    data = data.Where(x => x.IsAvailable == filter.IsAvailable.Value);
                }

                #endregion


                data = data.OrderByField(filter.PropertyName, filter.Ascending);

                if (filter.PageSize <= 0)
                {
                    filter.PageSize = 10;
                }

                int totalCount = await data.CountAsync(cancellationToken);

                int excludedRows = (filter.PageNumber - 1) * filter.PageSize;
                if (excludedRows < 0) excludedRows = 0;

                var listData = await data
                    .Skip(excludedRows)
                    .Take(filter.PageSize)
                    .ToListAsync(cancellationToken);

                return new PaginationList<InfoServiceModel>()
                {
                    DataCount = listData.Count,
                    TotalCount = totalCount,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize,
                    Data = listData
                };
            }
        }
    }
}