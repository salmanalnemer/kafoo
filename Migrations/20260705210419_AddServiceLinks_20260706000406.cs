using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kafo.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceLinks_20260706000406 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 220, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 600, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    TargetAudience = table.Column<string>(type: "TEXT", maxLength: 180, nullable: true),
                    ButtonText = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    ImagePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    OpenInNewTab = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceLinks", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceLinks");
        }
    }
}
