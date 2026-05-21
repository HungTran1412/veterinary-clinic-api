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
    public class DeleteShiftTemplateCommand : IRequest<Unit>
    {
        public int Id { get; set; }

        public DeleteShiftTemplateCommand(int id)
        {
            Id = id;
        }

        public class Handler : IRequestHandler<DeleteShiftTemplateCommand, Unit>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly ICacheService _cacheService;
            private readonly IStringLocalizer<DeleteShiftTemplateCommand> _localizer;
            private readonly IContextAccessor _contextAccessor;

            public Handler(VeterinaryClinicDataContext dataContext, ICacheService cacheService, IStringLocalizer<DeleteShiftTemplateCommand> localizer, Func<IContextAccessor> contextAccessorFactory)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
                _localizer = localizer;
                _contextAccessor = contextAccessorFactory();
            }

            public async Task<Unit> Handle(DeleteShiftTemplateCommand request, CancellationToken cancellationToken)
            {
                if (_contextAccessor.Role != Role.ADMIN.ToString())
                {
                    throw new ArgumentException(_localizer["forbidden"]);
                }

                var id = request.Id;
                Log.Information($"Delete {ShiftTemplateConstant.CachePrefix}: {id}");
                
                var dt = await _dataContext.VcShiftTemplates.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
                if (dt == null)
                {
                    throw new ArgumentException($"{_localizer["data.not_found"]}");
                }

                dt.ModifiedUserId = _contextAccessor.UserId;
                dt.IsActive = false;
                
                await _dataContext.SaveChangesAsync(cancellationToken);
    
                _cacheService.Remove(ShiftTemplateConstant.BuildCacheKey(id.ToString()));
                _cacheService.Remove(ShiftTemplateConstant.BuildCacheKey());

                Log.Information($"Delete {ShiftTemplateConstant.CachePrefix} completed");
                
                return Unit.Value;
            }
        }
    }
}