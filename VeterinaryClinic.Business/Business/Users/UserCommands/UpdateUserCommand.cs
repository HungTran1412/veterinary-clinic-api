using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using VeterinaryClinic.Shared.ContextAccessor;

namespace VeterinaryClinic.Business
{
    public class UpdateUserCommand : IRequest<Unit>
    {
        public UpdateUserModel Model { get; }

        /// <summary>
        /// Cap nhat thong tin
        /// </summary>
        /// <param name="model"></param>
        public UpdateUserCommand(UpdateUserModel model)
        {
            Model = model;
        }

        public class Handler : IRequestHandler<UpdateUserCommand, Unit>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly ICacheService _cacheService;
            private readonly IStringLocalizer<UpdateUserCommand> _localizer; // Sửa generic type cho đúng class
            private readonly IContextAccessor _contextAccessor;

            public Handler(VeterinaryClinicDataContext dataContext, ICacheService cacheService, IStringLocalizer<UpdateUserCommand> localizer, Func<IContextAccessor> contextAccessorFactory)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
                _localizer = localizer;
                _contextAccessor = contextAccessorFactory();
            }

            public async Task<Unit> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
            {
                var model = request.Model;
                Log.Information($"Update User: " + JsonSerializer.Serialize(model));
                
                //Kiem tra ton tai khong
                var entity = await _dataContext.VcUsers.FirstOrDefaultAsync(x => x.Id == model.Id, cancellationToken);
                if (entity == null)
                {
                    throw new ArgumentException($"{_localizer["data.not-found"]}");
                }
                
                //validate mail
                if (!ValidationUtils.IsValidEmail(model.Email))
                {
                    throw new ArgumentException($"{_localizer["user.invalid.email_format"]}");
                }

                #region check duplicate

                // Kiểm tra trùng Email (loại trừ chính nó)
                var isEmailExisted = await _dataContext.VcUsers.AnyAsync(x => x.Email == model.Email && x.Id != model.Id, cancellationToken);
                if (isEmailExisted)
                {
                    throw new ArgumentException($"{_localizer["user.existed.email"]}");
                }

                // Kiểm tra trùng số điện thoại (loại trừ chính nó)
                var isPhoneExisted = await _dataContext.VcUsers.AnyAsync(x => x.PhoneNumber == model.PhoneNumber && x.Id != model.Id, cancellationToken);
                if (isPhoneExisted)
                {
                    // Bạn cần thêm key này vào json: "user.existed.phone_number": "Số điện thoại đã tồn tại"
                    throw new ArgumentException($"{_localizer["user.existed.phone_number"]}");
                }

                #endregion
                
                
                //cap nhat thong tin
                model.UpdateEntity(entity);
                
                //luu vao db
                _dataContext.VcUsers.Update(entity);
                await _dataContext.SaveChangesAsync(cancellationToken);
                
                //xoa cache
                _cacheService.Remove(UserConstant.BuildCacheKey(entity.Id.ToString()));
                _cacheService.Remove(UserConstant.BuildCacheKey());
                
                return Unit.Value;
            }
        }
    }
}
