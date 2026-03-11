using MediatR;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Data;

namespace VeterinaryClinic.Business
{
    public class GetComboboxServiceQuery : IRequest<List<ServiceSelectItemModel>>
    {
        public int Count { get; }
        public string TextSearch { get; }

        /// <summary>
        /// Lay danh sach dich vu
        /// </summary>
        /// <param name="count">So luong ban ghi can lay</param>
        /// <param name="textSearch">Tu khoa tim kiem</param>
        public GetComboboxServiceQuery(int count, string textSearch)
        {
            Count = count;
            TextSearch = textSearch;
        }

        public class Handler : IRequestHandler<GetComboboxServiceQuery, List<ServiceSelectItemModel>>
        {
            private readonly VeterinaryClinicReadDataContext _dataContext;
            private readonly ICacheService _cacheService;

            public Handler(VeterinaryClinicReadDataContext dataContext, ICacheService cacheService)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
            }

            public async Task<List<ServiceSelectItemModel>> Handle(GetComboboxServiceQuery request,
                CancellationToken cancellationToken)
            {
                var count = request.Count;
                var textSearch = request.TextSearch;

                string cacheKey = ServiceConstant.BuildCacheKey();
                var list = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var data = (from item in _dataContext.VcServices
                        .AsNoTracking()
                        .Where(x => x.IsActive && x.IsAvailable)
                        .OrderBy(x => x.Order)
                        .ThenBy(x => x.Name)
                            select new ServiceSelectItemModel()
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