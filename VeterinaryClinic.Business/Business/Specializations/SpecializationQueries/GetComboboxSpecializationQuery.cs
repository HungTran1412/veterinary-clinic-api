using MediatR;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Data;

namespace VeterinaryClinic.Business
{
    public class GetComboboxSpecializationQuery : IRequest<List<SpecializationSelectItemModel>>
    {
        public int Count { get; }
        public string TextSearch { get; }

        /// <summary>
        /// Lay danh sach chuyen nganh
        /// </summary>
        /// <param name="count">So luong bann ghi lay ra</param>
        /// <param name="textSearch">Tu khoa tim kiem</param>
        public GetComboboxSpecializationQuery(int count, string textSearch)
        {
            Count = count;
            TextSearch = textSearch;
        }

        public class Handler : IRequestHandler<GetComboboxSpecializationQuery, List<SpecializationSelectItemModel>>
        {
            private readonly VeterinaryClinicReadDataContext _dataContext;
            private readonly ICacheService _cacheService;

            public Handler(VeterinaryClinicReadDataContext dataContext, ICacheService cacheService)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
            }

            public async Task<List<SpecializationSelectItemModel>> Handle(GetComboboxSpecializationQuery request,
                CancellationToken cancellationToken)
            {
                var count = request.Count;
                var textSearch = request.TextSearch;

                string cacheKey = SpecializationConstant.BuildCacheKey();
                var list = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var data = (from item in _dataContext.VcSpecializations
                        .AsNoTracking()
                        .Where(x => x.IsActive)
                        .OrderBy(x => x.Order)
                        .ThenBy(x => x.Name)
                            select new SpecializationSelectItemModel()
                            {
                                Id = item.Id,
                                Name = item.Name,
                                Code = item.Code
                            });
                    return await data.ToListAsync();
                });

                if (!string.IsNullOrEmpty(textSearch))
                {
                    textSearch = textSearch.ToLower().Trim();
                    list = list.Where(x => 
                        x.Name.ToLower().Contains(textSearch) ||
                        (x.Code != null && x.Code.ToLower().Contains(textSearch))).ToList();
                }

                if (count > 0)
                {
                    list = list.Take(count).ToList();
                }

                return list;
            }
        }
    }   
}