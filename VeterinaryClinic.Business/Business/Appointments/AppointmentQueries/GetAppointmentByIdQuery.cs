using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

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

            public Handler(VeterinaryClinicReadDataContext dataContext, ICacheService cacheService)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
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
                                    SerivceId = a.SerivceId,
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

                return item;
            }
        }
    }
}
