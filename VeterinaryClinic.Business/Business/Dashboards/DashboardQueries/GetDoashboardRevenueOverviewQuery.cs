using MediatR;
using Microsoft.Extensions.Localization;
using Serilog;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class GetDoashboardRevenueOverviewQuery : IRequest<RevenueOverviewModel>
    {
        public RevenueOverviewRequestModel Model { get; }

        /// <summary>
        /// Thong ke doanh thu 
        /// </summary>
        /// <param name="month"></param>
        public GetDoashboardRevenueOverviewQuery(RevenueOverviewRequestModel model)
        {
            Model = model;
        }

        public class Handler : IRequestHandler<GetDoashboardRevenueOverviewQuery, RevenueOverviewModel>
        {
            private readonly IStringLocalizer<GetMedicalRecordByIdQuery> _localizer;
            private readonly IVeterinaryClinicCallStoreHelper _callStoreHelper;
            private readonly IContextAccessor _contextAccessor;

            public Handler(IStringLocalizer<GetMedicalRecordByIdQuery> localizer, IVeterinaryClinicCallStoreHelper callStoreHelper, Func<IContextAccessor> contextAccessorFactory)
            {
                _localizer = localizer;
                _callStoreHelper = callStoreHelper;
                _contextAccessor = contextAccessorFactory();
            }

            public async Task<RevenueOverviewModel> Handle(GetDoashboardRevenueOverviewQuery request, CancellationToken cancellationToken)
            {
                var model = request.Model;
                Log.Information($"Checking role for GetDoashboardRevenueOverviewQuery. Current user role: {_contextAccessor.Role}"); // Thêm dòng log này

                if (_contextAccessor.Role != Role.ADMIN.ToString())
                {
                    throw new ArgumentException(_localizer["dashboard.no-permission"]);
                }
                
                var dataTable = _callStoreHelper.CallStoreDashboardRevenueOverviewAsync(model.Month, model.Year);
                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    throw new KeyNotFoundException(_localizer["data.not_found"]);
                }

                var statisticInfo = dataTable.Rows[0].ToObject<RevenueOverviewModel>();
                return await Task.FromResult(statisticInfo);
            }
        }
    }   
}