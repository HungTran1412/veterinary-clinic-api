using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{ 
    public class DashboardBaseModel
    {
    
    }

    public record OverviewStatisticModel
    {
        /// <summary>
        /// tong lich kham
        /// </summary>
        public long TotalAppointment { get; init; }
        
        /// <summary>
        /// tong doanh thu
        /// </summary>
        public decimal TotalRevenue { get; init; }
        
        /// <summary>
        /// tong khach hang
        /// </summary>
        public long TotalCustomer { get; init; }
        
        /// <summary>
        /// tong thu cung
        /// </summary>
        public long TotalPet { get; init; }
    }

    public record RevenueOverviewModel
    {
        #region Today

        [DataColumn("today_total_revenue")]
        public decimal TodayTotalRevenue { get; init; }
        [DataColumn("today_total_invoices")]
        public long TodayTotalInvoice { get; init; }
        [DataColumn("today_total_services_used")]
        public long TodayTotalServiceUsed { get; init; }

        #endregion

        #region Week

        [DataColumn("week_total_revenue")]
        public decimal WeekTotalRevenue { get; init; }
        [DataColumn("week_total_invoices")]
        public long WeekTotalInvoice { get; init; }
        [DataColumn("week_total_services_used")]
        public long WeekTotalServiceUsed { get; init; }

        #endregion

        #region Month

        [DataColumn("month_total_revenue")]
        public decimal MonthTotalRevenue { get; init; }
        [DataColumn("month_total_invoices")]
        public long MonthTotalInvoice { get; init; }
        [DataColumn("month_total_services_used")]
        public long MonthTotalServiceUsed { get; init; }

        #endregion

        #region Year

        [DataColumn("year_total_revenue")]
        public decimal YearTotalRevenue { get; init; }
        [DataColumn("year_total_invoices")]
        public long YearTotalInvoice { get; init; }
        [DataColumn("year_total_services_used")]
        public long YearTotalServiceUsed { get; init; }

        #endregion
    }
}

