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
    public class UserRegisterCommand : IRequest<Unit>
    {
        public UserRegisterModel Model { get; }

        public UserRegisterCommand(UserRegisterModel model)
        {
            Model = model;
        }

        public class Handler : IRequestHandler<UserRegisterCommand, Unit>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly IStringLocalizer<UserRegisterCommand> _localizer;
            private readonly IBcryptPasswordHasher _passwordHasher;
            private readonly IEmailService _emailService;
            private readonly MailSettings _mailSettings;

            public Handler(
                VeterinaryClinicDataContext dataContext,
                IStringLocalizer<UserRegisterCommand> localizer,
                IBcryptPasswordHasher passwordHasher,
                IEmailService emailService,
                IOptions<MailSettings> mailSettings)
            {
                _dataContext = dataContext;
                _localizer = localizer;
                _passwordHasher = passwordHasher;
                _emailService = emailService;
                _mailSettings = mailSettings.Value;
            }

            public async Task<Unit> Handle(UserRegisterCommand request, CancellationToken cancellationToken)
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
                    Code = GenerateCodeUtils.GenerateUserCode("CUS"),
                    Username = model.UserName,
                    Email = model.Email,
                    Password = _passwordHasher.HashPassword(model.Password),
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    Role = Role.CUSTOMER.ToString(),
                    IsActive = false,
                    VerificationToken = verificationToken,
                    VerificationTokenExpires = DateTime.UtcNow.AddDays(1) // Token valid for 1 day
                };

                await _dataContext.VcUsers.AddAsync(entity, cancellationToken);
                await _dataContext.SaveChangesAsync(cancellationToken);

                // Gửi email xác thực
                try
                {
                    var verificationLink = $"{_mailSettings.BaseUrl}/veterinary-clinic/v1/authorization/verify-email?token={verificationToken}";
                    string subject = "Xác thực tài khoản của bạn - Phòng khám thú y";
                    string body = EmailTemplates.GetVerificationEmail(entity.FullName, verificationLink);

                    await _emailService.SendEmailAsync(entity.Email, subject, body);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Failed to send verification email to {entity.Email}");
                }

                Log.Information($"User {model.UserName} registered successfully. Verification email sent.");

                return Unit.Value;
            }
        }
    }
}
