using MediatR;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business;
public class GetPermissionByCodeQuery : IRequest<PermissionModel>
    {
        public string Code { get; set; }

        /// <summary>
        /// Lấy thông tin quyền người dùng theo code
        /// </summary>
        /// <param name="code">Code quyền người dùng</param>
        public GetPermissionByCodeQuery(string code)
        {
            Code = code;
        }

        public class Handler : IRequestHandler<GetPermissionByCodeQuery, PermissionModel>
        {
            private readonly VeterinaryClinicReadDataContext _dataContext;
            private readonly ICacheService _cacheService;

            public Handler(VeterinaryClinicReadDataContext dataContext, ICacheService cacheService)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
            }

            public async Task<PermissionModel> Handle(GetPermissionByCodeQuery request, CancellationToken cancellationToken)
            {
                string cacheKey = PermissionConstant.BuildCacheKey($"code-{request.Code}");
                var item = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var entity = await _dataContext.Permissions.FirstOrDefaultAsync(x => x.Code == request.Code);
                    return AutoMapperUtils.AutoMap<Permission, PermissionModel>(entity);
                });
                return item;
            }
        }
    }

    public class GetComboboxPermissionQuery : IRequest<List<PermissionSelectItemModel>>
    {
        public int Count { get; set; }
        public string TextSearch { get; set; }
        public int? IdPhanHe { get; set; }

        /// <summary>
        /// Lấy danh sách quyền người dùng cho combobox
        /// </summary>
        /// <param name="count">Số lượng bản ghi cần lấy ra</param>
        /// <param name="textSearch">Từ khóa tìm kiếm</param>
        public GetComboboxPermissionQuery(int count = 0, string textSearch = "", int? idPhanHe = null)
        {
            this.Count = count;
            this.TextSearch = textSearch;
            this.IdPhanHe = idPhanHe;
        }

        public class Handler : IRequestHandler<GetComboboxPermissionQuery, List<PermissionSelectItemModel>>
        {
            private readonly VeterinaryClinicReadDataContext _dataContext;
            private readonly ICacheService _cacheService;
            private readonly IMediator _mediator;

            public Handler(VeterinaryClinicReadDataContext dataContext, ICacheService cacheService, IMediator mediator)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
                _mediator = mediator;
            }

            public async Task<List<PermissionSelectItemModel>> Handle(GetComboboxPermissionQuery request, CancellationToken cancellationToken)
            {
                string cacheKey = PermissionConstant.BuildCacheKey();
                var list = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var data = (from item in _dataContext.Permissions.Where(x => x.IsActive).OrderBy(x => x.Order).ThenBy(x => x.Name)
                                select new PermissionSelectItemModel()
                                {
                                    Id = item.Id,
                                    Code = item.Code,
                                    Name = item.Name,
                                    GroupName = item.GroupName
                                });

                    return await data.ToListAsync();
                });

                if (!string.IsNullOrEmpty(request.TextSearch))
                {
                    request.TextSearch = request.TextSearch.ToLower().Trim();
                    list = list.Where(x => x.Name.ToLower().Contains(request.TextSearch) || x.Note.ToLower().Contains(request.TextSearch)).ToList();
                }

                if (request.IdPhanHe.HasValue && request.IdPhanHe != 0)
                {
                    list = list.Where(x => x.IdPhanHe == request.IdPhanHe).ToList();
                }

                if (request.Count > 0)
                {
                    list = list.Take(request.Count).ToList();
                }
                
                return list;
            }
        }
    }
