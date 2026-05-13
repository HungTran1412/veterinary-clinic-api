using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{

    public class SendOtpCommand : IRequest<SendOtpResponseModel>
    {
        public ForgotPasswordModel Model { get; }

        /// <summary>
        /// tìm tài khoản và gửi otp đổi mật khẩu
        /// </summary>
        /// <param name="model"></param>
        public SendOtpCommand(ForgotPasswordModel model)
        {
            Model = model;
        }

        public class Handler : IRequestHandler<SendOtpCommand, SendOtpResponseModel>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly IStringLocalizer<SendOtpCommand> _localizer;
            private readonly MailSettings _mailSettings;

            public Handler(
                VeterinaryClinicDataContext dataContext,
                IStringLocalizer<SendOtpCommand> localizer,
                IOptions<MailSettings> mailSettings)
            {
                _dataContext = dataContext;
                _localizer = localizer;
                _mailSettings = mailSettings.Value;
            }

            public async Task<SendOtpResponseModel> Handle(SendOtpCommand request, CancellationToken cancellationToken)
            {
                var model = request.Model;
                Log.Information($"Send OTP request for: {model.LoginIdentifier}");

                var user = await _dataContext.VcUsers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                            x.IsActive &&
                            (x.Username == model.LoginIdentifier || x.Email == model.LoginIdentifier || x.PhoneNumber == model.LoginIdentifier),
                        cancellationToken);

                if (user == null)
                {
                    throw new ArgumentException($"{_localizer["data.not-found"]}");
                }

                // Generate a 6-digit OTP
                var otp = new Random().Next(100000, 999999).ToString("D6");

                var verificationToken = new VcUserVerificationTokens
                {
                    UserId = user.Id,
                    Token = otp,
                    TokenType = TokenType.OTP.ToString(),
                    ExpirationAt = DateTime.UtcNow.AddMinutes(5),
                    IsUsed = false,
                    Code = GenerateCodeUtils.GenerateUserCode("OTP")
                };

                await _dataContext.VcUserVerificationTokens.AddAsync(verificationToken, cancellationToken);
                await _dataContext.SaveChangesAsync(cancellationToken);

                // Send OTP email using Hangfire
                string subject = "Mã OTP để đặt lại mật khẩu của bạn";
                string body = EmailTemplates.GetOtpEmail(user.FullName, otp);
                BackgroundJob.Enqueue<IEmailService>(emailService => emailService.SendEmailAsync(user.Email, subject, body));

                Log.Information($"OTP email job enqueued for {user.Email}.");

                return new SendOtpResponseModel
                {
                    Email = user.Email,
                };
            }
        }
    }
    
    public class VerifyOtpCommand : IRequest<Unit>
    {
        public VerifyOtpModel Model { get; }

        /// <summary>
        /// Xác thực otp
        /// </summary>
        /// <param name="model"></param>
        public VerifyOtpCommand(VerifyOtpModel model)
        {
            Model = model;
        }

        public class Handler : IRequestHandler<VerifyOtpCommand, Unit>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly IStringLocalizer<VerifyOtpCommand> _localizer;

            public Handler(VeterinaryClinicDataContext dataContext, IStringLocalizer<VerifyOtpCommand> localizer)
            {
                _dataContext = dataContext;
                _localizer = localizer;
            }

            public async Task<Unit> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
            {
                var otp = request.Model.Otp;
                Log.Information($"Verifying OTP: {otp}");

                var verificationToken = await _dataContext.VcUserVerificationTokens
                    .FirstOrDefaultAsync(t => t.Token == otp && t.TokenType == TokenType.OTP.ToString() && !t.IsUsed, cancellationToken);

                if (verificationToken == null)
                {
                    throw new ArgumentException(_localizer["otp.invalid"]);
                }

                if (verificationToken.ExpirationAt < DateTime.UtcNow)
                {
                    _dataContext.VcUserVerificationTokens.Remove(verificationToken);
                    await _dataContext.SaveChangesAsync(cancellationToken);
                    throw new ArgumentException(_localizer["otp.expired"]);
                }

                verificationToken.IsUsed = true;
                verificationToken.UsedDate = DateTime.UtcNow;
                _dataContext.VcUserVerificationTokens.Update(verificationToken);
                await _dataContext.SaveChangesAsync(cancellationToken);

                Log.Information($"OTP {otp} verified successfully.");

                return Unit.Value;
            }
        }
    }
    
    public class ResetPasswordCommand : IRequest<Unit>
    {
        public ResetPasswordModel Model { get; }

        /// <summary>
        /// cập nhật mật khẩu mới
        /// </summary>
        /// <param name="model"></param>
        public ResetPasswordCommand(ResetPasswordModel model)
        {
            Model = model;
        }

        public class Handler : IRequestHandler<ResetPasswordCommand, Unit>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly IStringLocalizer<ResetPasswordCommand> _localizer;
            private readonly IBcryptPasswordHasher _passwordHasher;

            public Handler(VeterinaryClinicDataContext dataContext, IStringLocalizer<ResetPasswordCommand> localizer, IBcryptPasswordHasher passwordHasher)
            {
                _dataContext = dataContext;
                _localizer = localizer;
                _passwordHasher = passwordHasher;
            }

            public async Task<Unit> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
            {
                var model = request.Model;
                Log.Information($"Reset password attempt for email: {model.Email}");

                if (model.NewPassword != model.ConfirmPassword)
                {
                    throw new ArgumentException(_localizer["user.password.mismatch"]);
                }

                if (!ValidationUtils.IsValidPassword(model.NewPassword))
                {
                    throw new ArgumentException(_localizer["user.invalid.password_complexity"]);
                }

                var user = await _dataContext.VcUsers.FirstOrDefaultAsync(u => u.Email == model.Email, cancellationToken);
                if (user == null)
                {
                    throw new ArgumentException(_localizer["user.not_found"]);
                }

                var verificationToken = await _dataContext.VcUserVerificationTokens
                    .Where(t => t.UserId == user.Id && t.TokenType == TokenType.OTP.ToString() && t.IsUsed)
                    .OrderByDescending(t => t.UsedDate)
                    .FirstOrDefaultAsync(cancellationToken);

                if (verificationToken == null)
                {
                    throw new ArgumentException(_localizer["otp.not_verified"]);
                }

                if (verificationToken.UsedDate.HasValue && verificationToken.UsedDate.Value.AddMinutes(10) < DateTime.UtcNow)
                {
                    _dataContext.VcUserVerificationTokens.Remove(verificationToken);
                    await _dataContext.SaveChangesAsync(cancellationToken);
                    throw new ArgumentException(_localizer["otp.session_expired"]);
                }

                user.Password = _passwordHasher.HashPassword(model.NewPassword);
                _dataContext.VcUsers.Update(user);

                _dataContext.VcUserVerificationTokens.Remove(verificationToken);

                await _dataContext.SaveChangesAsync(cancellationToken);

                Log.Information($"Password has been reset successfully for user {user.Email}.");

                return Unit.Value;
            }
        }
    }
}
