using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kafo.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAidReports_20260706075608 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AidReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    ReportType = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    PeriodLabel = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    BeneficiariesCount = table.Column<int>(type: "INTEGER", nullable: false),
                    FamiliesCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 900, nullable: true),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CoverImagePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsFeatured = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AidReports", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AidReports");
        }
    }
}
