using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using System.Text.Json;
using VeterinaryClinic.Business.Services; // Interface giờ nằm ở đây
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using VeterinaryClinic.Shared.ContextAccessor;
using VeterinaryClinic.Shared.Templates;

namespace VeterinaryClinic.Business
{
    public class CreateUserCommand : IRequest<Unit>
    {
        public CreateUserModel Model { get; }

        /// <summary>
        /// Them moi nguoi dung
        /// </summary>
        /// <param name="model"></param>
        public CreateUserCommand(CreateUserModel model)
        {
            Model = model;
        }

        public class Handler : IRequestHandler<CreateUserCommand, Unit>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly ICacheService _cacheService;
            private readonly IStringLocalizer<CreateUserCommand> _localizer;
            private readonly IContextAccessor _contextAccessor;
            private readonly IBcryptPasswordHasher _passwordHasher;
            private readonly IEmailService _emailService;

            public Handler(
                VeterinaryClinicDataContext dataContext, 
                ICacheService cacheService, 
                IStringLocalizer<CreateUserCommand> localizer, 
                Func<IContextAccessor> contextAccessorFactory,
                IBcryptPasswordHasher passwordHasher,
                IEmailService emailService)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
                _localizer = localizer;
                _contextAccessor = contextAccessorFactory();
                _passwordHasher = passwordHasher;
                _emailService = emailService;
            }

            public async Task<Unit> Handle(CreateUserCommand request, CancellationToken cancellationToken)
            {
                var model = request.Model;
                Log.Information($"Create User: " + JsonSerializer.Serialize(model));

                #region Validate

                //validate email
                if (!ValidationUtils.IsValidEmail(model.Email))
                {
                    throw new ArgumentException($"{_localizer["user.invalid.email_format"]}");
                }

                //validate password
                if (!ValidationUtils.IsValidPassword(model.Password))
                {
                    // Mật khẩu phải có chữ hoa, chữ thường, số và ký tự đặc biệt
                    throw new ArgumentException($"{_localizer["user.invalid.password_complexity"]}");
                }

                #endregion

                var entity = AutoMapperUtils.AutoMap<CreateUserModel, VcUsers>(model);
                
                if (entity == null)
                {
                    throw new ArgumentException("Failed to map data.");
                }

                #region Check Duplicate
                var checkCode = await _dataContext.VcUsers.AnyAsync(x => x.Code == entity.Code, cancellationToken);
                if (checkCode)
                {
                    throw new ArgumentException($"{_localizer["user.existed.code"]}");
                }

                var checkUsername = await _dataContext.VcUsers.AnyAsync(x => x.Username == entity.Username, cancellationToken);
                if (checkUsername)
                {
                    throw new ArgumentException($"{_localizer["user.existed.username"]}");
                }

                var checkEmail = await _dataContext.VcUsers.AnyAsync(x => x.Email == entity.Email, cancellationToken);
                if (checkEmail)
                {
                    throw new ArgumentException($"{_localizer["user.existed.email"]}");
                }

                #endregion

                string plainPassword = model.Password;
                entity.Password = _passwordHasher.HashPassword(plainPassword);

                await _dataContext.VcUsers.AddAsync(entity, cancellationToken);
                await _dataContext.SaveChangesAsync(cancellationToken);
                
                try 
                {
                    string subject = "Thông báo cấp tài khoản - Phòng khám thú y";
                    string body = EmailTemplates.GetAccountCreatedEmail(
                        entity.FullName, 
                        entity.Username, 
                        plainPassword, 
                        entity.Role
                    );

                    await _emailService.SendEmailAsync(entity.Email, subject, body);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Failed to send email to {entity.Email}");
                }

                _cacheService.Remove(UserConstant.BuildCacheKey());
                
                return Unit.Value;
            }
        }
    }   
}