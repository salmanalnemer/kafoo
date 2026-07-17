using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kafo.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddGeneralAssemblyMinutes_20260706084046 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GeneralAssemblyMinutes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    MeetingType = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    MeetingNumber = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    FiscalYear = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    MeetingDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AttendeesCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 900, nullable: true),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CoverImagePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsFeatured = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneralAssemblyMinutes", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GeneralAssemblyMinutes");
        }
    }
}
