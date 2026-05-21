using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeterinaryClinic.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateColumnNameInVcShiftTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StartTime",
                table: "vcShiftTemplates",
                newName: "start_time");

            migrationBuilder.RenameColumn(
                name: "ShiftName",
                table: "vcShiftTemplates",
                newName: "shift_name");

            migrationBuilder.RenameColumn(
                name: "MaxEmployee",
                table: "vcShiftTemplates",
                newName: "max_employee");

            migrationBuilder.RenameColumn(
                name: "EndTime",
                table: "vcShiftTemplates",
                newName: "end_time");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "start_time",
                table: "vcShiftTemplates",
                newName: "StartTime");

            migrationBuilder.RenameColumn(
                name: "shift_name",
                table: "vcShiftTemplates",
                newName: "ShiftName");

            migrationBuilder.RenameColumn(
                name: "max_employee",
                table: "vcShiftTemplates",
                newName: "MaxEmployee");

            migrationBuilder.RenameColumn(
                name: "end_time",
                table: "vcShiftTemplates",
                newName: "EndTime");
        }
    }
}
