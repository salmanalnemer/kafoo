using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kafo.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddLicenseDocuments_20260705232606 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LicenseDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 220, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 900, nullable: true),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    FileType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicenseDocuments", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LicenseDocuments");
        }
    }
}
