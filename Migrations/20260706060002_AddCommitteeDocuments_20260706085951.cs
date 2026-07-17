using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kafo.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCommitteeDocuments_20260706085951 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommitteeDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    Year = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 900, nullable: true),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommitteeDocuments", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommitteeDocuments");
        }
    }
}
