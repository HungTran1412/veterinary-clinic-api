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
}

