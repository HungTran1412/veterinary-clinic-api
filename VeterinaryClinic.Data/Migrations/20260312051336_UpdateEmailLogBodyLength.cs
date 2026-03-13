using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeterinaryClinic.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEmailLogBodyLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_vcDoctorSpecializations",
                table: "vcDoctorSpecializations");

            migrationBuilder.AlterColumn<string>(
                name: "body",
                table: "vcEmailLogs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AddPrimaryKey(
                name: "PK_vcDoctorSpecializations",
                table: "vcDoctorSpecializations",
                columns: new[] { "doctor_id", "specialization_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_vcDoctorSpecializations",
                table: "vcDoctorSpecializations");

            migrationBuilder.AlterColumn<string>(
                name: "body",
                table: "vcEmailLogs",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_vcDoctorSpecializations",
                table: "vcDoctorSpecializations",
                column: "id");
        }
    }
}
