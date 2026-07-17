using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kafo.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddInitiatives_20260706184403 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Initiatives",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    InitiativeType = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Location = table.Column<string>(type: "TEXT", maxLength: 220, nullable: true),
                    TargetGroup = table.Column<string>(type: "TEXT", maxLength: 220, nullable: true),
                    BeneficiariesCount = table.Column<int>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Objectives = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    RegistrationUrl = table.Column<string>(type: "TEXT", maxLength: 800, nullable: true),
                    ImagePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsFeatured = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Initiatives", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Initiatives");
        }
    }
}
