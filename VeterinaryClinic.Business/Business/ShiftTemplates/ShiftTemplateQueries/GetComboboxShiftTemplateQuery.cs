using MediatR;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Data;

namespace VeterinaryClinic.Business
{
    public class GetComboboxShiftTemplateQuery : IRequest<List<ShiftTemplateSelectItemModel>>
    {
        public int Count { get; }
        public string TextSearch { get; }

        public GetComboboxShiftTemplateQuery(int count, string textSearch)
        {
            Count = count;
            TextSearch = textSearch;
        }

        public class Handler : IRequestHandler<GetComboboxShiftTemplateQuery, List<ShiftTemplateSelectItemModel>>
        {
            private readonly VeterinaryClinicReadDataContext _dataContext;
            private readonly ICacheService _cacheService;

            public Handler(VeterinaryClinicReadDataContext dataContext, ICacheService cacheService)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
            }

            public async Task<List<ShiftTemplateSelectItemModel>> Handle(GetComboboxShiftTemplateQuery request,
                CancellationToken cancellationToken)
            {
                var count = request.Count;
                var textSearch = request.TextSearch;

                string cacheKey = ShiftTemplateConstant.BuildCacheKey();
                var list = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var data = (from item in _dataContext.VcShiftTemplates
                        .AsNoTracking()
                        .Where(x => x.IsActive)
                        .OrderBy(x => x.ShiftName)
                            select new ShiftTemplateSelectItemModel()
                            {
                                Id = item.Id,
                                Name = item.ShiftName,
                                Code = item.Code
                            });
                    return await data.ToListAsync(cancellationToken);
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