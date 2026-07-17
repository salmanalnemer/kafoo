using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kafo.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddHomeSectionsProgramsPartners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HomeStatistics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeStatistics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProgramProjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 900, nullable: true),
                    ImagePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    LinkUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramProjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StrategicGoals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 700, nullable: true),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategicGoals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SuccessPartners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    LogoPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuccessPartners", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HomeStatistics");

            migrationBuilder.DropTable(
                name: "ProgramProjects");

            migrationBuilder.DropTable(
                name: "StrategicGoals");

            migrationBuilder.DropTable(
                name: "SuccessPartners");
        }
    }
}
