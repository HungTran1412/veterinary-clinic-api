using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class DeleteServiceCommand : IRequest<Unit>
    {
        public int Id { get; set; }

        /// <summary>
        /// Xoa dich vu
        /// </summary>
        /// <param name="id">id dich vu can xoa</param>
        public DeleteServiceCommand(int id)
        {
            Id = id;
        }

        public class Handler : IRequestHandler<DeleteServiceCommand, Unit>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly ICacheService _cacheService;
            private readonly IStringLocalizer<DeleteServiceCommand> _localizer;
            private readonly IContextAccessor _contextAccessor;

            public Handler(VeterinaryClinicDataContext dataContext, ICacheService cacheService, IStringLocalizer<DeleteServiceCommand> localizer, Func<IContextAccessor> contextAccessorFactory)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
                _localizer = localizer;
                _contextAccessor = contextAccessorFactory();
            }

            public async Task<Unit> Handle(DeleteServiceCommand request, CancellationToken cancellationToken)
            {
                var id = request.Id;
                var currentUserId = _contextAccessor.UserId;
                var userRole = _contextAccessor.Role;
                Log.Information($"Delete Service {id} attempt by User {currentUserId}");

                // Security Check: Only ADMIN or RECEPTIONIST can delete.
                if (userRole != Role.ADMIN.ToString())
                {
                    throw new ArgumentException(_localizer["user.unauthorized"]);
                }
                
                //kiem tra data co ton tai khong
                var dt = await _dataContext.VcServices.FirstOrDefaultAsync(x => x.Id == id);
                if (dt == null)
                {
                    throw new ArgumentException($"{_localizer["data.not-found"]}");
                }

                //xoa mem
                dt.ModifiedUserId = currentUserId;
                dt.IsActive = false;
                
                //luu vao db
                await _dataContext.SaveChangesAsync(cancellationToken);
    
                //Xoa cache
                _cacheService.Remove(ServiceConstant.BuildCacheKey(id.ToString()));
                _cacheService.Remove(ServiceConstant.BuildCacheKey());

                Log.Information($"Delete Service {id} completed");
                
                return Unit.Value;
            }
        }
    }   
}