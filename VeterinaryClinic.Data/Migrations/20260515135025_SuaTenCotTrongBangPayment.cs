using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeterinaryClinic.Data.Migrations
{
    /// <inheritdoc />
    public partial class SuaTenCotTrongBangPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "vcPayments",
                newName: "amount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "amount",
                table: "vcPayments",
                newName: "Amount");
        }
    }
}
