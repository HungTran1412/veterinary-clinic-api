using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeterinaryClinic.Data.Migrations
{
    /// <inheritdoc />
    public partial class add_table_vcReadModelOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vcReadModelOutbox",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityType = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    KeyValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vcReadModelOutbox", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_vcReadModelOutbox_ProcessedAtUtc_Id",
                table: "vcReadModelOutbox",
                columns: new[] { "ProcessedAtUtc", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vcReadModelOutbox");
        }
    }
}
