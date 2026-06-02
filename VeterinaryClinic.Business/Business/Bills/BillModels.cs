using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VeterinaryClinic.Business
{
    public record CreateBillModel
    {
        [Required]
        [MinLength(1)]
        public List<int> AppointmentIds { get; init; } = new();

        [Required]
        public string PaymentMethod { get; init; } = string.Empty;

        public string? Note { get; init; }
        
        // For VNPay
        public string? ClientIpAddress { get; init; }
    }
    
    /// <summary>
    /// Model chứa toàn bộ dữ liệu cần thiết để vẽ một hóa đơn PDF.
    /// </summary>
    public record BillPdfModel
    {
        public string BillCode { get; init; } = string.Empty;
        public DateTime BillDate { get; init; }
        
        public string CustomerName { get; init; } = string.Empty;
        public string CustomerPhone { get; init; } = string.Empty;
        public string CustomerAddress { get; init; } = string.Empty;

        public List<BillPdfItemModel> Items { get; init; } = new();

        public decimal TotalAmount { get; init; }
        
        // Thông tin phòng khám (có thể lấy từ appsettings)
        public string ClinicName { get; init; } = "Phòng khám Thú y PetCare";
        public string ClinicAddress { get; init; } = "123 Đường ABC, Quận 1, TP. Hồ Chí Minh";
        public string ClinicPhone { get; init; } = "0123 456 789";
    }

    /// <summary>
    /// Model cho một dòng dịch vụ trong bảng chi tiết hóa đơn.
    /// </summary>
    public record BillPdfItemModel
    {
        public int Index { get; init; }
        public string ServiceName { get; init; } = string.Empty;
        public string PetName { get; init; } = string.Empty;
        public decimal Price { get; init; }
    }
}
