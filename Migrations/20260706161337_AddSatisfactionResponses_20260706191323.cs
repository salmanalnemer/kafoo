using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kafo.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSatisfactionResponses_20260706191323 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SatisfactionResponses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 220, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 180, nullable: true),
                    BeneficiaryType = table.Column<string>(type: "TEXT", maxLength: 140, nullable: false),
                    ServiceName = table.Column<string>(type: "TEXT", maxLength: 220, nullable: false),
                    SatisfactionLevel = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Rating = table.Column<int>(type: "INTEGER", nullable: false),
                    PositiveNotes = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: true),
                    ImprovementNotes = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: true),
                    Suggestions = table.Column<string>(type: "TEXT", maxLength: 1500, nullable: true),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    AdminNotes = table.Column<string>(type: "TEXT", maxLength: 1500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SatisfactionResponses", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SatisfactionResponses");
        }
    }
}
