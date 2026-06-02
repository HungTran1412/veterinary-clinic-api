using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VeterinaryClinic.Business;

namespace VeterinaryClinic.Infrastructure
{
    public class BillDocument : IDocument
    {
        private readonly BillPdfModel _model;

        public BillDocument(BillPdfModel model)
        {
            _model = model;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container
                .Page(page =>
                {
                    page.Margin(50);
                    page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Arial"));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(ComposeContent);
                    page.Footer().Element(ComposeFooter);
                });
        }

        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text(_model.ClinicName).Bold().FontSize(20);
                    column.Item().Text(_model.ClinicAddress);
                    column.Item().Text($"Điện thoại: {_model.ClinicPhone}");
                });

                if (_model.LogoImageBytes != null)
                {
                    row.ConstantItem(100).Image(_model.LogoImageBytes);
                }
            });
        }

        private void ComposeContent(IContainer container)
        {
            container.PaddingVertical(40).Column(column =>
            {
                column.Spacing(20);

                // Title
                column.Item().AlignCenter().Text("HÓA ĐƠN THANH TOÁN").SemiBold().FontSize(24).FontColor(Colors.Grey.Darken2);

                // Customer and Bill Info
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("KHÁCH HÀNG");
                        col.Item().Text("Tên khách hàng: " + _model.CustomerName).SemiBold();
                        col.Item().Text("Địa chỉ: " + _model.CustomerAddress);
                        col.Item().Text("SĐT: " + _model.CustomerPhone);
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().AlignRight().Text($"Số HĐ: {_model.BillCode}");
                        col.Item().AlignRight().Text($"Ngày HĐ: {_model.BillDate:dd/MM/yyyy}");
                    });
                });

                // Table
                column.Item().Element(ComposeTable);
                
                // Total
                column.Item().AlignRight().Text($"Tổng cộng: {_model.TotalAmount:N0} VNĐ").SemiBold().FontSize(16);
            });
        }

        private void ComposeTable(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(25);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Text("STT");
                    header.Cell().Text("Dịch vụ");
                    header.Cell().Text("Thú cưng");
                    header.Cell().AlignRight().Text("Thành tiền");
                    
                    header.Cell().ColumnSpan(4).PaddingTop(5).BorderBottom(1).BorderColor(Colors.Black);
                });

                foreach (var item in _model.Items)
                {
                    table.Cell().Text(item.Index.ToString());
                    table.Cell().Text(item.ServiceName);
                    table.Cell().Text(item.PetName);
                    table.Cell().AlignRight().Text($"{item.Price:N0} VNĐ");
                }
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Text(text =>
            {
                text.Span("Cảm ơn quý khách đã sử dụng dịch vụ của ").FontSize(10);
                text.Span(_model.ClinicName).SemiBold().FontSize(10);
                text.Span("!").FontSize(10);
            });
        }
    }
}
