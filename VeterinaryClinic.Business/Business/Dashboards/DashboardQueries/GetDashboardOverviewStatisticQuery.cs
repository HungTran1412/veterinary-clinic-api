using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using Serilog; // Thêm using cho Serilog

namespace VeterinaryClinic.Business
{
    public class GetDashboardOverviewStatisticQuery : IRequest<OverviewStatisticModel>
    {
        public class Handler : IRequestHandler<GetDashboardOverviewStatisticQuery, OverviewStatisticModel>
        {
            private readonly VeterinaryClinicReadDataContext _dataContext;
            private readonly ICacheService _cacheService;
            private readonly IStringLocalizer<GetDashboardOverviewStatisticQuery> _localizer;
            private readonly IContextAccessor _contextAccessor;

            public Handler(VeterinaryClinicReadDataContext dataContext, ICacheService cacheService, IStringLocalizer<GetDashboardOverviewStatisticQuery> localizer, Func<IContextAccessor> contextAccessorFactory)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
                _localizer = localizer;
                _contextAccessor = contextAccessorFactory();
            }

            public async Task<OverviewStatisticModel> Handle(GetDashboardOverviewStatisticQuery request, CancellationToken cancellationToken)
            {
                Log.Information($"Checking role for GetOverviewStatisticQuery. Current user role: {_contextAccessor.Role}"); // Thêm dòng log này

                if (_contextAccessor.Role != Role.ADMIN.ToString())
                {
                    throw new ArgumentException(_localizer["dashboard.no-permission"]);
                }
                
                // Lấy tổng số lịch hẹn đang hoạt động
                var totalAppointment = await _dataContext.VcAppointments
                    .AsNoTracking()
                    .CountAsync(a => a.IsActive, cancellationToken);

                // Tính tổng doanh thu từ các thanh toán thành công và đang hoạt động
                var totalRevenue = await _dataContext.VcPayments
                    .AsNoTracking()
                    .Where(p => p.IsActive && p.PaymentStatus == PaymentStatus.SUCCESS.ToString())
                    .SumAsync(p => p.Amount, cancellationToken);

                // Lấy tổng số khách hàng đang hoạt động
                var totalCustomer = await _dataContext.VcUsers
                    .AsNoTracking()
                    .CountAsync(u => u.IsActive && u.Role == Role.CUSTOMER.ToString(), cancellationToken);

                // Lấy tổng số thú cưng đang hoạt động
                var totalPet = await _dataContext.VcPets
                    .AsNoTracking()
                    .CountAsync(p => p.IsActive, cancellationToken);

                return new OverviewStatisticModel
                {
                    TotalAppointment = totalAppointment,
                    TotalRevenue = totalRevenue,
                    TotalCustomer = totalCustomer,
                    TotalPet = totalPet
                };
            }
        }
    }   
}