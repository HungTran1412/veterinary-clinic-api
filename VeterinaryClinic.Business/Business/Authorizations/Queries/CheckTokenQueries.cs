using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    /// <summary>
    /// Query to check if the current user's JWT is valid and return their information.
    /// This endpoint must be protected by [Authorize].
    /// </summary>
    public class CheckTokenQuery : IRequest<CheckTokenResponseModel>
    {
        // This query doesn't need any parameters as it relies on the context.
    }

    public class CheckTokenQueryHandler : IRequestHandler<CheckTokenQuery, CheckTokenResponseModel>
    {
        private readonly IContextAccessor _contextAccessor;
        private readonly VeterinaryClinicReadDataContext _dataContext;

        public CheckTokenQueryHandler(Func<IContextAccessor> contextAccessorFactory, VeterinaryClinicReadDataContext dataContext)
        {
            _contextAccessor = contextAccessorFactory();
            _dataContext = dataContext;
        }

        public async Task<CheckTokenResponseModel> Handle(CheckTokenQuery request, CancellationToken cancellationToken)
        {
            var userId = _contextAccessor.UserId;

            // If the token was invalid or expired, the middleware would have failed,
            // and UserId would be null.
            if (!userId.HasValue)
            {
                return null;
            }

            // The token is valid, so we can trust the UserId.
            // Fetch the user details from the database.
            var user = await _dataContext.VcUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);

            if (user == null)
            {
                // This is an edge case: the token is valid, but the user has been deleted.
                return null;
            }

            // Map the user entity to the response model.
            return new CheckTokenResponseModel
            {
                Id = user.Id,
                UserName = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                AvatarUrl = user.AvatarUrl
            };
        }
    }
}
