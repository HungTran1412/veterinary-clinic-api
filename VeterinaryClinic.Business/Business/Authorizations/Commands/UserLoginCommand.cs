using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using System.Security.Claims;
using VeterinaryClinic.Business;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared.ContextAccessor;

namespace VeterinaryClinic.Business
{
    public class UserLoginCommand : IRequest<LoginResponseModel>
    {
        public LoginModel Model { get; }

        /// <summary>
        /// Dang nhap he thong
        /// </summary>
        /// <param name="model"></param>
        public UserLoginCommand(LoginModel model)
        {
            Model = model;
        }

        public class Handler : IRequestHandler<UserLoginCommand, LoginResponseModel>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly IBcryptPasswordHasher _passwordHasher;
            private readonly IStringLocalizer<UserLoginCommand> _localizer;
            private readonly IJwtService _jwtService;

            public Handler(
                VeterinaryClinicDataContext dataContext, 
                IBcryptPasswordHasher passwordHasher,
                IStringLocalizer<UserLoginCommand> localizer,
                IJwtService jwtService)
            {
                _dataContext = dataContext;
                _passwordHasher = passwordHasher;
                _localizer = localizer;
                _jwtService = jwtService;
            }

            public async Task<LoginResponseModel> Handle(UserLoginCommand request, CancellationToken cancellationToken)
            {
                var model = request.Model;
                Log.Information($"User Login Attempt: {model.LoginIdentifier}");

                var user = await _dataContext.VcUsers
                    .FirstOrDefaultAsync(x => 
                        x.IsActive && 
                        (x.Username == model.LoginIdentifier || x.Email == model.LoginIdentifier || x.PhoneNumber == model.LoginIdentifier), 
                        cancellationToken);

                if (user == null || !_passwordHasher.VerifyPassword(model.Password, user.Password))
                {
                    Log.Warning($"Login failed for user: {model.LoginIdentifier}");
                    throw new UnauthorizedAccessException($"{_localizer["user.login.failed"]}");
                }

                // Tạo claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                // Sinh token
                var accessToken = _jwtService.GenerateAccessToken(claims);
                var refreshToken = _jwtService.GenerateRefreshToken();

                // Lưu refresh token vào DB
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.Now.AddDays(7); // Cấu hình sau
                _dataContext.VcUsers.Update(user);
                await _dataContext.SaveChangesAsync(cancellationToken);

                Log.Information($"User {user.Username} logged in successfully.");

                return new LoginResponseModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    UserName = user.Username,
                    Email = user.Email,
                    Role = user.Role,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                };
            }
        }
    }
}
