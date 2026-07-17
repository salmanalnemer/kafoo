using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kafo.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddJobApplications_20260706185408 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 220, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    NationalId = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    City = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    DesiredJobTitle = table.Column<string>(type: "TEXT", maxLength: 220, nullable: true),
                    Qualification = table.Column<string>(type: "TEXT", maxLength: 220, nullable: true),
                    Specialty = table.Column<string>(type: "TEXT", maxLength: 220, nullable: true),
                    ExperienceYears = table.Column<int>(type: "INTEGER", nullable: false),
                    CoverLetter = table.Column<string>(type: "TEXT", maxLength: 1500, nullable: true),
                    CvFilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    AttachmentFilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    AdminNotes = table.Column<string>(type: "TEXT", maxLength: 1500, nullable: true),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobApplications", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobApplications");
        }
    }
}
