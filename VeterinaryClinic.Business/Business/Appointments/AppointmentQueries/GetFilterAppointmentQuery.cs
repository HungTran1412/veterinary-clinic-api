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
            private readonly IAppointmentStateMachine _stateMachine;
            private readonly IContextAccessor _contextAccessor;

            public Handler(VeterinaryClinicReadDataContext dataContext, IStringLocalizer<GetFilterAppointmentQuery> localizer, IAppointmentStateMachine stateMachine, Func<IContextAccessor> contextAccessorFactory)
            {
                _dataContext = dataContext;
                _localizer = localizer;
                _stateMachine = stateMachine;
                _contextAccessor = contextAccessorFactory();
            }

            public async Task<PaginationList<AppointmentModel>> Handle(GetFilterAppointmentQuery request,
                CancellationToken cancellationToken)
            {
                var filter = request.Filter;
                var role = Enum.Parse<Role>(_contextAccessor.Role);
                var userId = _contextAccessor.UserId;

                #region Validate
                if (filter.FromDate.HasValue && filter.ToDate.HasValue && filter.FromDate > filter.ToDate)
                {
                    throw new ArgumentException(_localizer["appointment.filter.date_invalid"]);
                }
                #endregion

                var data = from a in _dataContext.VcAppointments.AsNoTracking()
                    //join bang user lay ten khach hang
                    join customer in _dataContext.VcUsers on a.CustomerId equals customer.Id into cus from customer in cus.DefaultIfEmpty()
                    
                    //join bang user lay ten bac si
                    join doctor in _dataContext.VcUsers on a.DoctorId equals doctor.Id into doc from doctor in doc.DefaultIfEmpty()
                    
                    //join bang service lay ten dich vu
                    join service in _dataContext.VcServices on a.ServiceId equals service.Id into ser from service in ser.DefaultIfEmpty()
                    
                    //join bang pet lay ten pet
                    join pet in _dataContext.VcPets on a.PetId equals pet.Id into 
                        p from pet in p.DefaultIfEmpty()
                    
                    //join bang medical record lay id
                    join mr in _dataContext.VcMedicalRecords on a.Id equals mr.AppointmentId into m from mr in m.DefaultIfEmpty()
                    
                    // LEFT JOIN to get BillId from Invoices
                    join inv in _dataContext.VcInvoices on a.Id equals inv.AppointmentId into invGroup
                    from invoice in invGroup.DefaultIfEmpty()
                    
                    where a.IsActive
                    select new AppointmentModel
                    {
                        Id = a.Id,
                        Code = a.Code,
                        CustomerId = a.CustomerId,
                        PetId = a.PetId,
                        ServiceId = a.ServiceId,
                        DoctorId = a.DoctorId,
                        MedicalRecordId = mr.Id,
                        BillId = invoice.BillId,
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
                        CreatedDate = a.CreatedDate, 
                        
                        CustomerName = customer.FullName,
                        CustomerPhone = customer.PhoneNumber,
                        DoctorName = doctor.FullName,
                        PetName = pet.Name,
                        ServiceName = service.Name,
                        ServicePrice = service.Price + " VNĐ"
                    };
                
                // if (role == Role.DOCTOR)
                // {
                //     data = data.Where(x => x.DoctorId == userId);
                // }
                // else 
                if (role == Role.CUSTOMER)
                {
                    data = data.Where(x => x.CustomerId == userId);
                }

                if (!string.IsNullOrEmpty(filter.TextSearch))
                {
                    string ts = filter.TextSearch.Trim().ToLower();
                    data = data.Where(x => x.Code != null && x.Code.ToLower().Contains(ts) 
                                           || x.CustomerName != null && x.CustomerName.ToLower().Contains(ts) 
                                           || x.PetName != null && x.PetName.ToLower().Contains(ts) 
                                           || x.CustomerPhone != null && x.CustomerPhone.ToLower().Contains(ts)
                                           || x.ServiceName != null && x.ServiceName.ToLower().Contains(ts));
                }

                #region Filter

                if (filter.IsActive.HasValue)
                {
                    data = data.Where(x => x.IsActive == filter.IsActive.Value);
                }

                if (filter.ServiceId.HasValue && filter.ServiceId > 0)
                {
                    data = data.Where(x => x.ServiceId == filter.ServiceId.Value);
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

                data = data.OrderByDescending(x => x.AppointmentDate).ThenByDescending(x=>x.Id);

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
                
                var currentRole = role;
                listData = listData.Select(item =>
                {
                    if (!Enum.TryParse<AppointmentStatus>(item.State, true, out var status))
                        return item;

                    return item with
                    {
                        Commands = _stateMachine
                            .GetAvailableActions(status, currentRole)
                            .Select(action => new WorkflowCommandModel(
                                action.ToString(),
                                _stateMachine.GetActionDisplayName(action)
                            ))
                            .ToList()
                    };
                }).ToList();
                
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
