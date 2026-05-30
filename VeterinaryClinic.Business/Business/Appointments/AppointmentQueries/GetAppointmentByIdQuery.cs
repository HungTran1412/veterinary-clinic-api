using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using VeterinaryClinic.Shared.ContextAccessor;

namespace VeterinaryClinic.Business
{
    public class GetAppointmentByIdQuery : IRequest<AppointmentModel>
    {
        public int Id { get; }

        public GetAppointmentByIdQuery(int id)
        {
            Id = id;
        }

        public class Handler : IRequestHandler<GetAppointmentByIdQuery, AppointmentModel>
        {
            private readonly VeterinaryClinicReadDataContext _dataContext;
            private readonly ICacheService _cacheService;
            private readonly IAppointmentStateMachine _stateMachine;
            private readonly IContextAccessor _contextAccessor;

            public Handler(
                VeterinaryClinicReadDataContext dataContext,
                ICacheService cacheService,
                IAppointmentStateMachine stateMachine,
                Func<IContextAccessor> contextAccessorFactory)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
                _stateMachine = stateMachine;
                _contextAccessor = contextAccessorFactory();
            }

            public async Task<AppointmentModel> Handle(GetAppointmentByIdQuery request, CancellationToken cancellationToken)
            {
                var id = request.Id;
                string cacheKey = AppointmentConstant.BuildCacheKey(id.ToString());

                var item = await _cacheService.GetOrCreate<AppointmentModel>(cacheKey, async () =>
                {
                    var query = from a in _dataContext.VcAppointments.AsNoTracking()
                                where a.Id == id
                                select new AppointmentModel
                                {
                                    Id = a.Id,
                                    Code = a.Code,
                                    CustomerId = a.CustomerId,
                                    PetId = a.PetId,
                                    ServiceId = a.ServiceId,
                                    DoctorId = a.DoctorId,
                                    AppointmentDate = a.AppointmentDate,
                                    StartTime = a.StartTime,
                                    EndTime = a.EndTime,
                                    CancelReason = a.CancelReason,
                                    Note = a.Note,
                                    State = a.State,
                                    StateName = a.StateName,
                                    IsFinalState = a.IsFinalState,
                                    ProcessId = a.ProcessId,
                                    AuthorId = a.AuthorId,
                                    Order = a.Order,
                                    IsActive = a.IsActive
                                };

                    return await query.FirstOrDefaultAsync(cancellationToken);
                });

                if (item != null &&
                    Enum.TryParse<AppointmentStatus>(item.State, true, out var status) &&
                    Enum.TryParse<Role>(_contextAccessor.Role, true, out var role))
                {
                    item = item with
                    {
                        Commands = _stateMachine
                            .GetAvailableActions(status, role)
                            .Select(action => new WorkflowCommandModel(
                                action.ToString(),
                                _stateMachine.GetActionDisplayName(action)
                            ))
                            .ToList()
                    };
                }

                return item;
            }
        }
    }
}
