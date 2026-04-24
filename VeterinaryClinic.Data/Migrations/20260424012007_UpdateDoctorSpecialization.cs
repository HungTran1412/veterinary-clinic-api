using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeterinaryClinic.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDoctorSpecialization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_date",
                table: "vcDoctorSpecializations");

            migrationBuilder.DropColumn(
                name: "created_user_id",
                table: "vcDoctorSpecializations");

            migrationBuilder.DropColumn(
                name: "created_user_name",
                table: "vcDoctorSpecializations");

            migrationBuilder.DropColumn(
                name: "id",
                table: "vcDoctorSpecializations");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "vcDoctorSpecializations");

            migrationBuilder.DropColumn(
                name: "modified_date",
                table: "vcDoctorSpecializations");

            migrationBuilder.DropColumn(
                name: "modified_user_id",
                table: "vcDoctorSpecializations");

            migrationBuilder.DropColumn(
                name: "modified_user_name",
                table: "vcDoctorSpecializations");

            migrationBuilder.DropColumn(
                name: "order",
                table: "vcDoctorSpecializations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "created_date",
                table: "vcDoctorSpecializations",
                type: "datetime2",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 103);

            migrationBuilder.AddColumn<int>(
                name: "created_user_id",
                table: "vcDoctorSpecializations",
                type: "int",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 104);

            migrationBuilder.AddColumn<string>(
                name: "created_user_name",
                table: "vcDoctorSpecializations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "")
                .Annotation("Relational:ColumnOrder", 105);

            migrationBuilder.AddColumn<int>(
                name: "id",
                table: "vcDoctorSpecializations",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("Relational:ColumnOrder", 1)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "vcDoctorSpecializations",
                type: "bit",
                nullable: false,
                defaultValue: false)
                .Annotation("Relational:ColumnOrder", 101);

            migrationBuilder.AddColumn<DateTime>(
                name: "modified_date",
                table: "vcDoctorSpecializations",
                type: "datetime2",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 106);

            migrationBuilder.AddColumn<int>(
                name: "modified_user_id",
                table: "vcDoctorSpecializations",
                type: "int",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 107);

            migrationBuilder.AddColumn<string>(
                name: "modified_user_name",
                table: "vcDoctorSpecializations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "")
                .Annotation("Relational:ColumnOrder", 108);

            migrationBuilder.AddColumn<int>(
                name: "order",
                table: "vcDoctorSpecializations",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("Relational:ColumnOrder", 100);
        }
    }
}
