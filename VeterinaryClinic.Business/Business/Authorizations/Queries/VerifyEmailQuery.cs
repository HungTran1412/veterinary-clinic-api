using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Serilog;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class VerifyEmailQuery : IRequest<string>
    {
        public string Token { get; }

        public VerifyEmailQuery(string token)
        {
            Token = token;
        }

        public class Handler : IRequestHandler<VerifyEmailQuery, string>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly IStringLocalizer<VerifyEmailQuery> _localizer;
            private readonly IEmailService _emailService;
            private readonly MailSettings _mailSettings;

            public Handler(
                VeterinaryClinicDataContext dataContext, 
                IStringLocalizer<VerifyEmailQuery> localizer,
                IEmailService emailService,
                IOptions<MailSettings> mailSettings)
            {
                _dataContext = dataContext;
                _localizer = localizer;
                _emailService = emailService;
                _mailSettings = mailSettings.Value;
            }

            public async Task<string> Handle(VerifyEmailQuery request, CancellationToken cancellationToken)
            {
                Log.Information($"Email verification attempt with token: {request.Token}");

                var user = await _dataContext.VcUsers.FirstOrDefaultAsync(u => u.VerificationToken == request.Token, cancellationToken);

                if (user == null)
                {
                    throw new ArgumentException(_localizer["user.verify.invalid_token"]);
                }

                if (user.IsActive)
                {
                    // Ném lỗi nhưng có thể coi là một trường hợp "thành công" vì tài khoản đã hoạt động
                    throw new ArgumentException(_localizer["user.verify.already_activated"]);
                }

                if (user.VerificationTokenExpires < DateTime.UtcNow)
                {
                    throw new ArgumentException(_localizer["user.verify.token_expired"]);
                }

                user.IsActive = true;
                user.VerificationToken = null; // Xóa token sau khi sử dụng
                user.VerificationTokenExpires = null;

                await _dataContext.SaveChangesAsync(cancellationToken);

                Log.Information($"User {user.Username} has been successfully activated.");

                // Gửi email thông báo đăng ký thành công
                try
                {
                    var loginUrl = $"{_mailSettings.ApiBaseUrl}/login"; // Giả sử URL đăng nhập là /login
                    string subject = "Tài khoản của bạn đã được kích hoạt - Phòng khám thú y";
                    string body = EmailTemplates.GetRegistrationSuccessEmail(user.FullName, loginUrl);

                    await _emailService.SendEmailAsync(user.Email, subject, body);
                    Log.Information($"Registration success email sent to {user.Email}");
                }
                catch (Exception ex)
                {
                    // Không ném lỗi ra ngoài để không làm gián đoạn luồng chính
                    Log.Error(ex, $"Failed to send registration success email to {user.Email}");
                }

                return _localizer["user.verify.success"];
            }
        }
    }
}
