using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Security.Claims;
using VeterinaryClinic.Data;

namespace VeterinaryClinic.Business
{
    public class RefreshTokenCommand : IRequest<LoginResponseModel>
    {
        public RefreshTokenModel Model { get; }

        public RefreshTokenCommand(RefreshTokenModel model)
        {
            Model = model;
        }

        public class Handler : IRequestHandler<RefreshTokenCommand, LoginResponseModel>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly IJwtService _jwtService;
            private readonly IStringLocalizer<RefreshTokenCommand> _localizer;

            public Handler(VeterinaryClinicDataContext dataContext, IJwtService jwtService, IStringLocalizer<RefreshTokenCommand> localizer)
            {
                _dataContext = dataContext;
                _jwtService = jwtService;
                _localizer = localizer;
            }

            public async Task<LoginResponseModel> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
            {
                var principal = _jwtService.GetPrincipalFromExpiredToken(request.Model.AccessToken);
                var userId = int.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                var user = await _dataContext.VcUsers.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

                if (user == null || user.RefreshToken != request.Model.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.Now)
                {
                    throw new UnauthorizedAccessException("Invalid refresh token");
                }

                var newAccessToken = _jwtService.GenerateAccessToken(principal.Claims);
                var newRefreshToken = _jwtService.GenerateRefreshToken();

                user.RefreshToken = newRefreshToken;
                user.RefreshTokenExpiryTime = DateTime.Now.AddDays(7);
                
                _dataContext.VcUsers.Update(user);
                await _dataContext.SaveChangesAsync(cancellationToken);

                return new LoginResponseModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    UserName = user.Username,
                    Email = user.Email,
                    Role = user.Role,
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken
                };
            }
        }
    }
}
