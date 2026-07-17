using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kafo.Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdateHomeProgramsNewsPartners_20260705221502 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LinkUrl",
                table: "ProgramProjects",
                newName: "ExternalUrl");

            migrationBuilder.AddColumn<string>(
                name: "Beneficiaries",
                table: "ProgramProjects",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Details",
                table: "ProgramProjects",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sector",
                table: "ProgramProjects",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NewsPosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 900, nullable: true),
                    Details = table.Column<string>(type: "TEXT", nullable: true),
                    ImagePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    Audience = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    PublishDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsPosts", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NewsPosts");

            migrationBuilder.DropColumn(
                name: "Beneficiaries",
                table: "ProgramProjects");

            migrationBuilder.DropColumn(
                name: "Details",
                table: "ProgramProjects");

            migrationBuilder.DropColumn(
                name: "Sector",
                table: "ProgramProjects");

            migrationBuilder.RenameColumn(
                name: "ExternalUrl",
                table: "ProgramProjects",
                newName: "LinkUrl");
        }
    }
}
