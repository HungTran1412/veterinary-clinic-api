using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class DeleteSpecializationCommand : IRequest<Unit>
    {
        public int Id { get; set; }

        /// <summary>
        /// Xoa chuyen nganh
        /// </summary>
        /// <param name="id">id chuyen nganh can xoa</param>
        public DeleteSpecializationCommand(int id)
        {
            Id = id;
        }

        public class Handler : IRequestHandler<DeleteSpecializationCommand, Unit>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly ICacheService _cacheService;
            private readonly IStringLocalizer<CreateSpecializationCommand> _localizer;
            private readonly IContextAccessor _contextAccessor;

            public Handler(VeterinaryClinicDataContext dataContext, ICacheService cacheService, IStringLocalizer<CreateSpecializationCommand> localizer, Func<IContextAccessor> contextAccessorFactory)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
                _localizer = localizer;
                _contextAccessor = contextAccessorFactory();
            }

            public async Task<Unit> Handle(DeleteSpecializationCommand request, CancellationToken cancellationToken)
            {
                var id = request.Id;
                Log.Information($"Delete {SpecializationConstant.CachePrefix}: {id}");
                
                //kiem tra data co ton tai khong
                var dt = await _dataContext.VcSpecializations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
                if (dt == null)
                {
                    throw new ArgumentException($"{_localizer["data.not-found"]}");
                }

                // Kiem tra xem co dich vu nao su dung chuyen nganh nay khong
                var isUsed = await _dataContext.VcServices.AnyAsync(s => s.SpecializationId == id && s.IsActive, cancellationToken);
                if (isUsed)
                {
                    throw new InvalidOperationException("Không thể xóa chuyên ngành vì có dịch vụ đang sử dụng.");
                }

                //xoa mem
                dt.ModifiedUserId = _contextAccessor.UserId;
                dt.IsActive = false;
                
                //luu vao db
                await _dataContext.SaveChangesAsync(cancellationToken);
    
                //Xoa cache
                _cacheService.Remove(SpecializationConstant.BuildCacheKey(id.ToString()));
                _cacheService.Remove(SpecializationConstant.BuildCacheKey());

                Log.Information($"Delete {SpecializationConstant.CachePrefix} completed");
                
                return Unit.Value;
            }
        }
    }   
}