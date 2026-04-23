using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using VeterinaryClinic.Shared.ContextAccessor;

namespace VeterinaryClinic.Business
{
    public class GetComboboxPetQuery : IRequest<List<SelectItemModel>>
    {
        public int? OwnerId { get; set; }
    }

    public class GetComboboxPetQueryHandler : IRequestHandler<GetComboboxPetQuery, List<SelectItemModel>>
    {
        private readonly VeterinaryClinicReadDataContext _dataContext;
        private readonly IContextAccessor _contextAccessor;

        public GetComboboxPetQueryHandler(VeterinaryClinicReadDataContext dataContext, Func<IContextAccessor> contextAccessorFactory)
        {
            _dataContext = dataContext;
            _contextAccessor = contextAccessorFactory();
        }

        public async Task<List<SelectItemModel>> Handle(GetComboboxPetQuery request, CancellationToken cancellationToken)
        {
            var query = _dataContext.VcPets.AsNoTracking().Where(p => p.IsActive);
            var currentUserId = _contextAccessor.UserId;
            var userRole = _contextAccessor.Role;

            // If the user is a CUSTOMER, only show their own pets.
            if (userRole == Role.CUSTOMER.ToString())
            {
                query = query.Where(p => p.OwnerId == currentUserId);
            }
            // For other roles, if an OwnerId is specified, filter by it.
            else if (request.OwnerId.HasValue)
            {
                query = query.Where(p => p.OwnerId == request.OwnerId.Value);
            }

            var pets = await query
                .OrderBy(p => p.Name)
                .Select(p => new SelectItemModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Code = p.Code
                })
                .ToListAsync(cancellationToken);

            return pets;
        }
    }
}
