using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeterinaryClinic.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vcAppointments",
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
                    customer_id = table.Column<int>(type: "int", nullable: false),
                    pet_id = table.Column<int>(type: "int", nullable: false),
                    service_id = table.Column<int>(type: "int", nullable: false),
                    doctor_id = table.Column<int>(type: "int", nullable: false),
                    appointment_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    start_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    end_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    cancel_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    note = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    author_id = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    process_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    state = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    state_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    is_final_state = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vcAppointments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vcDoctorSpecializations",
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
                    doctor_id = table.Column<int>(type: "int", nullable: false),
                    specialization_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vcDoctorSpecializations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vcEmailLogs",
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
                    to_email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    subject = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    body = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    sent_status = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    error_message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    reference_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    reference_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vcEmailLogs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vcInvoices",
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
                    appointment_id = table.Column<int>(type: "int", nullable: false),
                    total_amount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    paid_date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vcInvoices", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vcMedicalRecords",
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
                    appointment_id = table.Column<int>(type: "int", nullable: false),
                    doctor_id = table.Column<int>(type: "int", nullable: false),
                    symptoms = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    diagnosis = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    treatment_plan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    prescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    doctor_note = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    completed_date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vcMedicalRecords", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vcNotifications",
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
                    user_id = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    is_read = table.Column<bool>(type: "bit", nullable: false),
                    related_entity_id = table.Column<int>(type: "int", nullable: false),
                    related_entity_type = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vcNotifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vcPets",
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
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    species = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    breed = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    gender = table.Column<bool>(type: "bit", nullable: false),
                    is_neutered = table.Column<bool>(type: "bit", nullable: false),
                    birth_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    weight = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    image_url = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    owner_id = table.Column<int>(type: "int", nullable: false),
                    note = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vcPets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vcServices",
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
                    name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    duration_minutes = table.Column<int>(type: "int", nullable: false),
                    specialization_id = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    is_available = table.Column<bool>(type: "bit", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vcServices", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vcSpecializations",
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
                    name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vcSpecializations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vcUsers",
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
                    username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    full_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    phone_number = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    avatar_url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    role = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vcUsers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vcUserVerificationTokens",
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
                    user_id = table.Column<int>(type: "int", nullable: false),
                    token = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    token_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    expiration_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_used = table.Column<bool>(type: "bit", nullable: false),
                    used_date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vcUserVerificationTokens", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vcWorkSchedules",
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
                    user_id = table.Column<int>(type: "int", nullable: false),
                    work_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    start_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    end_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    shift_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    note = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vcWorkSchedules", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vcAppointments");

            migrationBuilder.DropTable(
                name: "vcDoctorSpecializations");

            migrationBuilder.DropTable(
                name: "vcEmailLogs");

            migrationBuilder.DropTable(
                name: "vcInvoices");

            migrationBuilder.DropTable(
                name: "vcMedicalRecords");

            migrationBuilder.DropTable(
                name: "vcNotifications");

            migrationBuilder.DropTable(
                name: "vcPets");

            migrationBuilder.DropTable(
                name: "vcServices");

            migrationBuilder.DropTable(
                name: "vcSpecializations");

            migrationBuilder.DropTable(
                name: "vcUsers");

            migrationBuilder.DropTable(
                name: "vcUserVerificationTokens");

            migrationBuilder.DropTable(
                name: "vcWorkSchedules");
        }
    }
}
