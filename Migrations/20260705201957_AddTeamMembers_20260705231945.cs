using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kafo.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamMembers_20260705231945 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeamMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    JobTitle = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    Bio = table.Column<string>(type: "TEXT", maxLength: 900, nullable: true),
                    ImagePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMembers", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeamMembers");
        }
    }
}
