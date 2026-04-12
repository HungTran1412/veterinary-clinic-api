using MediatR;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class GetComboboxUserQuery : IRequest<List<UserSelectItemModel>>
    {
        public int Count { get; }
        public string TextSearch { get; }

        public GetComboboxUserQuery(int count, string textSearch)
        {
            Count = count;
            TextSearch = textSearch;
        }

        public class Handler : IRequestHandler<GetComboboxUserQuery, List<UserSelectItemModel>>
        {
            private readonly VeterinaryClinicReadDataContext _dataContext;
            private readonly ICacheService _cacheService;

            public Handler(VeterinaryClinicReadDataContext dataContext, ICacheService cacheService)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
            }

            public async Task<List<UserSelectItemModel>> Handle(GetComboboxUserQuery request,
                CancellationToken cancellationToken)
            {
                var count = request.Count;
                var textSearch = request.TextSearch;

                string cacheKey = "UserCombobox"; // Define a cache key
                var list = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var rolesToInclude = new[] { Role.DOCTOR.ToString(), Role.RECEPTIONIST.ToString() };
                    var data = await _dataContext.VcUsers
                        .AsNoTracking()
                        .Where(x => x.IsActive && rolesToInclude.Contains(x.Role))
                        .OrderBy(x => x.Order)
                        .ThenBy(x => x.FullName)
                        .Select(item => new UserSelectItemModel()
                        {
                            Id = item.Id,
                            Name = item.FullName,
                            Code = item.Code,
                            Role = item.Role
                        }).ToListAsync(cancellationToken);
                    return data;
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
