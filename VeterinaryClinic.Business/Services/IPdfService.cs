namespace VeterinaryClinic.Business
{
    /// <summary>
    /// Interface cho dịch vụ tạo file PDF.
    /// </summary>
    public interface IPdfService
    {
        /// <summary>
        /// Tạo file PDF cho một hóa đơn tổng dựa trên dữ liệu được cung cấp.
        /// </summary>
        /// <param name="model">Dữ liệu của hóa đơn.</param>
        /// <returns>Một mảng byte chứa nội dung của file PDF.</returns>
        byte[] GenerateBillPdf(BillPdfModel model);
    }
}
