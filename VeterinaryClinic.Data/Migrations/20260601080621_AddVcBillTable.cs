using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeterinaryClinic.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVcBillTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vcBills",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    order = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_user_id = table.Column<int>(type: "int", nullable: true),
                    created_user_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    modified_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    modified_user_id = table.Column<int>(type: "int", nullable: true),
                    modified_user_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    customer_id = table.Column<int>(type: "int", nullable: false),
                    total_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    bill_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    note = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vcBills", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vcBills");
        }
    }
}
