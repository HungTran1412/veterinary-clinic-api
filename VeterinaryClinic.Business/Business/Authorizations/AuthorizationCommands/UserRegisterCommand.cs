using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Serilog;
using System.Security.Cryptography;
using System.Text.Json;
using VeterinaryClinic.Business;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Busines
{
    public record UserRegisterResponseModel
    {
        public string Email { get; init; }
    }

    public class UserRegisterCommand : IRequest<UserRegisterResponseModel>
    {
        public UserRegisterModel Model { get; }

        public UserRegisterCommand(UserRegisterModel model)
        {
            Model = model;
        }

        public class Handler : IRequestHandler<UserRegisterCommand, UserRegisterResponseModel>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly IStringLocalizer<UserRegisterCommand> _localizer;
            private readonly IBcryptPasswordHasher _passwordHasher;
            private readonly MailSettings _mailSettings;
            private readonly ICacheService _cacheService;

            public Handler(
                VeterinaryClinicDataContext dataContext,
                IStringLocalizer<UserRegisterCommand> localizer,
                IBcryptPasswordHasher passwordHasher,
                IOptions<MailSettings> mailSettings,
                ICacheService cacheService)
            {
                _dataContext = dataContext;
                _localizer = localizer;
                _passwordHasher = passwordHasher;
                _mailSettings = mailSettings.Value;
                _cacheService = cacheService;
            }

            public async Task<UserRegisterResponseModel> Handle(UserRegisterCommand request, CancellationToken cancellationToken)
            {
                var model = request.Model;
                Log.Information($"User Registration attempt: " + JsonSerializer.Serialize(model));

                #region Validation
                if (model.Password != model.RepeatPassword)
                {
                    throw new ArgumentException(_localizer["user.password.mismatch"]);
                }

                if (!ValidationUtils.IsValidUsername(model.UserName))
                {
                    throw new ArgumentException(_localizer["user.invalid.username"]);
                }

                if (!ValidationUtils.IsValidEmail(model.Email))
                {
                    throw new ArgumentException(_localizer["user.invalid.email_format"]);
                }

                if (!ValidationUtils.IsValidPassword(model.Password))
                {
                    throw new ArgumentException(_localizer["user.invalid.password_complexity"]);
                }

                var isEmailExisted = await _dataContext.VcUsers.AnyAsync(x => x.Email == model.Email, cancellationToken);
                if (isEmailExisted)
                {
                    throw new ArgumentException(_localizer["user.existed.email"]);
                }

                var isUsernameExisted = await _dataContext.VcUsers.AnyAsync(x => x.Username == model.UserName, cancellationToken);
                if (isUsernameExisted)
                {
                    throw new ArgumentException(_localizer["user.existed.username"]);
                }

                var isPhoneExisted = await _dataContext.VcUsers.AnyAsync(x => x.PhoneNumber == model.PhoneNumber, cancellationToken);
                if (isPhoneExisted)
                {
                    throw new ArgumentException(_localizer["user.existed.phone_number"]);
                }
                #endregion

                var verificationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));

                var entity = new VcUsers
                {
                    Code = GenerateCodeUtils.GenerateCode("CUS"),
                    Username = model.UserName,
                    Email = model.Email,
                    Password = _passwordHasher.HashPassword(model.Password),
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    Role = Role.CUSTOMER.ToString(),
                    AvatarUrl = string.Empty,
                    IsActive = false,
                    VerificationToken = verificationToken,
                    VerificationTokenExpires = DateTime.UtcNow.AddDays(1),
                    
                    CreatedDate = DateTime.UtcNow,
                    CreatedUserName = model.UserName
                };

                await _dataContext.VcUsers.AddAsync(entity, cancellationToken);
                await _dataContext.SaveChangesAsync(cancellationToken);

                // Gửi email xác thực sử dụng Hangfire
                var frontendUrl = _mailSettings.FrontendBaseUrl.TrimEnd('/');
                var verificationLink = $"{frontendUrl}/verify-email?token={verificationToken}";
                string subject = "Xác thực tài khoản của bạn - Phòng khám thú y";
                string body = EmailTemplates.GetVerificationEmail(entity.FullName, verificationLink);

                BackgroundJob.Enqueue<IEmailService>(emailService => emailService.SendEmailAsync(entity.Email, subject, body));

                Log.Information($"User {model.UserName} registered successfully. Verification email job enqueued.");

                _cacheService.Remove(AuthorizationConstant.BuildCacheKey());
                
                return new UserRegisterResponseModel { Email = entity.Email };
            }
        }
    }
}