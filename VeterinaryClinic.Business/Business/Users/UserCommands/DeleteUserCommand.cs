using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{

    public class DeleteUserCommand : IRequest<Unit>
    {
        public int Id { get; }

        /// <summary>
        /// Xoa nguoi dung
        /// </summary>
        /// <param name="id">id nguoi dung can xoa</param>
        public DeleteUserCommand(int id)
        {
            Id = id;
        }
        
        public class Handler : IRequestHandler<DeleteUserCommand, Unit>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly ICacheService _cacheService;
            private readonly IStringLocalizer<DeleteUserCommand> _localizer;
            private readonly IContextAccessor _contextAccessor;

            public Handler(VeterinaryClinicDataContext dataContext, ICacheService cacheService, IStringLocalizer<DeleteUserCommand> localizer, Func<IContextAccessor> contextAccessorFactory)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
                _localizer = localizer;
                _contextAccessor = contextAccessorFactory();
            }

            public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
            {
                var id = request.Id;
                Log.Information($"Delete {UserConstant.CachePrefix}: {id}");
                
                //kiem tra data co ton tai khong
                var dt = await _dataContext.VcUsers.FirstOrDefaultAsync(x => x.Id == id);
                if (dt == null)
                {
                    throw new ArgumentException($"{_localizer["data.not-found"]}");
                }

                //xoa mem
                dt.ModifiedUserId = _contextAccessor.UserId;
                dt.IsActive = false;
                
                //luu vao db
                await _dataContext.SaveChangesAsync(cancellationToken);
    
                //Xoa cache
                _cacheService.Remove(UserConstant.BuildCacheKey(id.ToString()));
                _cacheService.Remove(UserConstant.BuildCacheKey());

                Log.Information($"Delete {UserConstant.CachePrefix} completed");
                
                return Unit.Value;
            }
        }
    }
    
}