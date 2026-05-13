using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeterinaryClinic.Data.Migrations
{
    /// <inheritdoc />
    public partial class ModifiedColNameInVcPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Code",
                table: "vcPayments",
                newName: "code");

            migrationBuilder.RenameColumn(
                name: "ResponseCode",
                table: "vcPayments",
                newName: "response_code");

            migrationBuilder.RenameColumn(
                name: "PaymentStatus",
                table: "vcPayments",
                newName: "payment_status");

            migrationBuilder.RenameColumn(
                name: "PaymentMethod",
                table: "vcPayments",
                newName: "payment_method");

            migrationBuilder.RenameColumn(
                name: "PaymentDate",
                table: "vcPayments",
                newName: "payment_date");

            migrationBuilder.RenameColumn(
                name: "InvoiceId",
                table: "vcPayments",
                newName: "invoice_id");

            migrationBuilder.RenameColumn(
                name: "GatewayTransactionId",
                table: "vcPayments",
                newName: "gateway_transaction_id");

            migrationBuilder.RenameColumn(
                name: "GatewayResponse",
                table: "vcPayments",
                newName: "gateway_response");

            migrationBuilder.AlterColumn<string>(
                name: "payment_status",
                table: "vcPayments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "payment_method",
                table: "vcPayments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "code",
                table: "vcPayments",
                newName: "Code");

            migrationBuilder.RenameColumn(
                name: "response_code",
                table: "vcPayments",
                newName: "ResponseCode");

            migrationBuilder.RenameColumn(
                name: "payment_status",
                table: "vcPayments",
                newName: "PaymentStatus");

            migrationBuilder.RenameColumn(
                name: "payment_method",
                table: "vcPayments",
                newName: "PaymentMethod");

            migrationBuilder.RenameColumn(
                name: "payment_date",
                table: "vcPayments",
                newName: "PaymentDate");

            migrationBuilder.RenameColumn(
                name: "invoice_id",
                table: "vcPayments",
                newName: "InvoiceId");

            migrationBuilder.RenameColumn(
                name: "gateway_transaction_id",
                table: "vcPayments",
                newName: "GatewayTransactionId");

            migrationBuilder.RenameColumn(
                name: "gateway_response",
                table: "vcPayments",
                newName: "GatewayResponse");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentStatus",
                table: "vcPayments",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentMethod",
                table: "vcPayments",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }
    }
}
