using MediatR;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Data;

namespace VeterinaryClinic.Business
{
    public class UserLogoutCommand : IRequest<Unit>
    {
        public LogoutModel Model { get; }

        /// <summary>
        /// đăng xuất hệ thống
        /// </summary>
        /// <param name="model"></param>
        public UserLogoutCommand(LogoutModel model)
        {
            Model = model;
        }

        public class Handler : IRequestHandler<UserLogoutCommand, Unit>
        {
            private readonly VeterinaryClinicDataContext _dataContext;

            public Handler(VeterinaryClinicDataContext dataContext)
            {
                _dataContext = dataContext;
            }

            public async Task<Unit> Handle(UserLogoutCommand request, CancellationToken cancellationToken)
            {
                var user = await _dataContext.VcUsers
                    .FirstOrDefaultAsync(u => u.RefreshToken == request.Model.RefreshToken, cancellationToken);

                if (user != null)
                {
                    user.RefreshToken = null;
                    user.RefreshTokenExpiryTime = null;
                    _dataContext.VcUsers.Update(user);
                    await _dataContext.SaveChangesAsync(cancellationToken);
                }

                return Unit.Value;
            }
        }
    }
}
