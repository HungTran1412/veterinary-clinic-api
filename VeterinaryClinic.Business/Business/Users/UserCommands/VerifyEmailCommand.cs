using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using VeterinaryClinic.Data;

namespace VeterinaryClinic.Business
{
    public class VerifyEmailCommand : IRequest<string>
    {
        public string Token { get; }

        public VerifyEmailCommand(string token)
        {
            Token = token;
        }

        public class Handler : IRequestHandler<VerifyEmailCommand, string>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly IStringLocalizer<VerifyEmailCommand> _localizer;

            public Handler(VeterinaryClinicDataContext dataContext, IStringLocalizer<VerifyEmailCommand> localizer)
            {
                _dataContext = dataContext;
                _localizer = localizer;
            }

            public async Task<string> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
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

                return _localizer["user.verify.success"];
            }
        }
    }
}
