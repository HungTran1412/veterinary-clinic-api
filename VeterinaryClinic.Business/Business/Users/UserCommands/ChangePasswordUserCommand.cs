using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using VeterinaryClinic.Business;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using VeterinaryClinic.Shared.ContextAccessor;

namespace VeterinaryClinic.Business
{
    public class ChangePasswordUserCommand : IRequest<Unit>
    {
        public UpdatePasswordUserModel Model { get; }

        /// <summary>
        /// doi mat khau
        /// </summary>
        /// <param name="model"></param>
        public ChangePasswordUserCommand(UpdatePasswordUserModel model)
        {
            Model = model;
        }

        public class Handler : IRequestHandler<ChangePasswordUserCommand, Unit>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly ICacheService _cacheService;
            private readonly IStringLocalizer<ChangePasswordUserCommand> _localizer;
            private readonly IContextAccessor _contextAccessor;
            private readonly IBcryptPasswordHasher _passwordHasher;

            public Handler(
                VeterinaryClinicDataContext dataContext, 
                ICacheService cacheService, 
                IStringLocalizer<ChangePasswordUserCommand> localizer, 
                Func<IContextAccessor> contextAccessorFactory,
                IBcryptPasswordHasher passwordHasher)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
                _localizer = localizer;
                _contextAccessor = contextAccessorFactory();
                _passwordHasher = passwordHasher;
            }

            public async Task<Unit> Handle(ChangePasswordUserCommand request, CancellationToken cancellationToken)
            {
                var model = request.Model;
                var userId = _contextAccessor.UserId;

                if (userId == null || userId == 0)
                {
                    throw new ArgumentException(_localizer["user.unauthorized"]);
                }

                Log.Information($"Change Password for User Id: {userId}");

                // Validate cơ bản
                if (string.IsNullOrEmpty(model.OldPassword) || string.IsNullOrEmpty(model.NewPassword) || string.IsNullOrEmpty(model.ConfirmPassword))
                {
                    throw new ArgumentException($"{_localizer["user.password.required"]}");
                }

                if (model.NewPassword != model.ConfirmPassword)
                {
                    throw new ArgumentException($"{_localizer["user.password.mismatch"]}");
                }

                if (!ValidationUtils.IsValidPassword(model.NewPassword))
                {
                    throw new ArgumentException($"{_localizer["user.invalid.password_complexity"]}");
                }

                // Lấy user từ DB (bao gồm cả mật khẩu hash)
                var user = await _dataContext.VcUsers.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
                
                if (user == null)
                {
                    throw new ArgumentException($"{_localizer["data.not-found"]}");
                }

                // Kiểm tra mật khẩu cũ
                if (!_passwordHasher.VerifyPassword(model.OldPassword, user.Password))
                {
                    throw new ArgumentException($"{_localizer["user.password.incorrect"]}");
                }

                // Cập nhật mật khẩu mới (Mã hóa)
                string newPasswordHash = _passwordHasher.HashPassword(model.NewPassword);
                
                // Sử dụng biểu thức with để cập nhật model một cách an toàn
                model = model with { ModifiedUserId = userId };
                user.Password = newPasswordHash;
                user.ModifiedUserId = userId;
                user.ModifiedDate = DateTime.Now;

                // Lưu vào DB
                _dataContext.VcUsers.Update(user);
                await _dataContext.SaveChangesAsync(cancellationToken);

                Log.Information($"Password changed successfully for User Id: {userId}");

                _cacheService.Remove(UserConstant.BuildCacheKey());
                
                return Unit.Value;
            }
        }
    }
}
