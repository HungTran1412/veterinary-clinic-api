using System;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
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
            private readonly IStringLocalizer<GetFilterServiceQuery> _localizer;

            public Handler(VeterinaryClinicReadDataContext dataContext, IStringLocalizer<GetFilterServiceQuery> localizer)
            {
                _dataContext = dataContext;
                _localizer = localizer;
            }

            public async Task<PaginationList<InfoServiceModel>> Handle(GetFilterServiceQuery request,
                CancellationToken cancellationToken)
            {
                var filter = request.Filter;

                #region Validate
                if (filter.MaxPrice.HasValue && filter.MinPrice.HasValue && filter.MaxPrice < filter.MinPrice)
                {
                    throw new ArgumentException(_localizer["service.filter.max_price_invalid"]);
                }

                if (filter.MaxDurationMinutes.HasValue && filter.MinDurationMinutes.HasValue && filter.MaxDurationMinutes < filter.MinDurationMinutes)
                {
                    throw new ArgumentException(_localizer["service.filter.max_duration_invalid"]);
                }
                #endregion

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
                        IsAvailable = s.IsAvailable,
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
                
                if (filter.MinPrice.HasValue)
                {
                    data = data.Where(x => x.Price >= filter.MinPrice.Value);
                }

                if (filter.MaxPrice.HasValue)
                {
                    data = data.Where(x => x.Price <= filter.MaxPrice.Value);
                }
                
                if (filter.MinDurationMinutes.HasValue)
                {
                    data = data.Where(x => x.DurationMinutes >= filter.MinDurationMinutes.Value);
                }

                if (filter.MaxDurationMinutes.HasValue)
                {
                    data = data.Where(x => x.DurationMinutes <= filter.MaxDurationMinutes.Value);
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