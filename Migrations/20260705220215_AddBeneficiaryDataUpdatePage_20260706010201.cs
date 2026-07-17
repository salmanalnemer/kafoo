using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kafo.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddBeneficiaryDataUpdatePage_20260706010201 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BeneficiaryDataUpdatePages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 220, nullable: false),
                    Subtitle = table.Column<string>(type: "TEXT", maxLength: 350, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    VideoSourceType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    YoutubeUrl = table.Column<string>(type: "TEXT", maxLength: 700, nullable: true),
                    VideoPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    AlertTitle = table.Column<string>(type: "TEXT", maxLength: 220, nullable: false),
                    AlertText = table.Column<string>(type: "TEXT", maxLength: 800, nullable: false),
                    PrimaryButtonText = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    PrimaryButtonUrl = table.Column<string>(type: "TEXT", maxLength: 700, nullable: false),
                    SecondaryButtonText = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    SecondaryButtonUrl = table.Column<string>(type: "TEXT", maxLength: 700, nullable: false),
                    OpenPrimaryInNewTab = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeneficiaryDataUpdatePages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BeneficiaryDataUpdateRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 220, nullable: false),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeneficiaryDataUpdateRequirements", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BeneficiaryDataUpdatePages");

            migrationBuilder.DropTable(
                name: "BeneficiaryDataUpdateRequirements");
        }
    }
}
