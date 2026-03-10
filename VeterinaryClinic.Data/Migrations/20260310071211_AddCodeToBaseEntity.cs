using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeterinaryClinic.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCodeToBaseEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "vcWorkSchedules",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "vcUserVerificationTokens",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "vcUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "vcSpecializations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "vcServices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "vcPets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "vcNotifications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "vcMedicalRecords",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "vcInvoices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "vcEmailLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "vcDoctorSpecializations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "vcAppointments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "code",
                table: "vcWorkSchedules");

            migrationBuilder.DropColumn(
                name: "code",
                table: "vcUserVerificationTokens");

            migrationBuilder.DropColumn(
                name: "code",
                table: "vcUsers");

            migrationBuilder.DropColumn(
                name: "code",
                table: "vcSpecializations");

            migrationBuilder.DropColumn(
                name: "code",
                table: "vcServices");

            migrationBuilder.DropColumn(
                name: "code",
                table: "vcPets");

            migrationBuilder.DropColumn(
                name: "code",
                table: "vcNotifications");

            migrationBuilder.DropColumn(
                name: "code",
                table: "vcMedicalRecords");

            migrationBuilder.DropColumn(
                name: "code",
                table: "vcInvoices");

            migrationBuilder.DropColumn(
                name: "code",
                table: "vcEmailLogs");

            migrationBuilder.DropColumn(
                name: "code",
                table: "vcDoctorSpecializations");

            migrationBuilder.DropColumn(
                name: "code",
                table: "vcAppointments");
        }
    }
}
