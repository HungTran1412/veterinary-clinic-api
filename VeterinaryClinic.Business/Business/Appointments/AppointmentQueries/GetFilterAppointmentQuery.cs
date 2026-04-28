using System;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class GetFilterAppointmentQuery : IRequest<PaginationList<AppointmentModel>>
    {
        public AppoinntmentFilterModel Filter { get; set; }

        /// <summary>
        /// Lay danh sach lich kham theo dieu kien loc
        /// </summary>
        /// <param name="filter">Thong tin loc</param>
        public GetFilterAppointmentQuery(AppoinntmentFilterModel filter)
        {
            Filter = filter;
        }

        public class Handler : IRequestHandler<GetFilterAppointmentQuery, PaginationList<AppointmentModel>>
        {
            private readonly VeterinaryClinicReadDataContext _dataContext;
            private readonly IStringLocalizer<GetFilterAppointmentQuery> _localizer;

            public Handler(VeterinaryClinicReadDataContext dataContext, IStringLocalizer<GetFilterAppointmentQuery> localizer)
            {
                _dataContext = dataContext;
                _localizer = localizer;
            }

            public async Task<PaginationList<AppointmentModel>> Handle(GetFilterAppointmentQuery request,
                CancellationToken cancellationToken)
            {
                var filter = request.Filter;

                #region Validate
                if (filter.FromDate.HasValue && filter.ToDate.HasValue && filter.FromDate > filter.ToDate)
                {
                    throw new ArgumentException(_localizer["appointment.filter.date_invalid"]);
                }
                #endregion

                var data = from a in _dataContext.VcAppointments.AsNoTracking()
                    where a.IsActive
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
                    };

                if (!string.IsNullOrEmpty(filter.TextSearch))
                {
                    string ts = filter.TextSearch.Trim().ToLower();
                    data = data.Where(x => x.Code != null && x.Code.ToLower().Contains(ts));
                }

                #region Filter

                if (filter.IsActive.HasValue)
                {
                    data = data.Where(x => x.IsActive == filter.IsActive.Value);
                }

                if (filter.CustomerId.HasValue && filter.CustomerId > 0)
                {
                    data = data.Where(x => x.CustomerId == filter.CustomerId.Value);
                }

                if (filter.PetId.HasValue && filter.PetId > 0)
                {
                    data = data.Where(x => x.PetId == filter.PetId.Value);
                }

                if (filter.ServiceId.HasValue && filter.ServiceId > 0)
                {
                    data = data.Where(x => x.SerivceId == filter.ServiceId.Value);
                }

                if (filter.DoctorId.HasValue && filter.DoctorId > 0)
                {
                    data = data.Where(x => x.DoctorId == filter.DoctorId.Value);
                }

                if (filter.FromDate.HasValue)
                {
                    data = data.Where(x => x.AppointmentDate >= filter.FromDate.Value);
                }

                if (filter.ToDate.HasValue)
                {
                    data = data.Where(x => x.AppointmentDate <= filter.ToDate.Value);
                }

                if (!string.IsNullOrEmpty(filter.State))
                {
                    data = data.Where(x => x.State == filter.State);
                }

                #endregion

                data = data.OrderByField(filter.PropertyName, filter.Ascending);

                if (filter.PageSize <= 0)
                {
                    filter.PageSize = 10;
                }

                int totalCount = await data.CountAsync(cancellationToken);

                int excludedRows = (filter.PageNumber - 1) * filter.PageSize;
                if (excludedRows < 0) excludedRows = 0;

                var listData = await data
                    .Skip(excludedRows)
                    .Take(filter.PageSize)
                    .ToListAsync(cancellationToken);

                return new PaginationList<AppointmentModel>()
                {
                    DataCount = listData.Count,
                    TotalCount = totalCount,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize,
                    Data = listData
                };
            }
        }
    }
}