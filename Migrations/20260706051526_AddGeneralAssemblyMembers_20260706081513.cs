using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kafo.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddGeneralAssemblyMembers_20260706081513 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GeneralAssemblyMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 220, nullable: false),
                    MembershipType = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    PositionTitle = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    MembershipNumber = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    TermLabel = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    PhotoPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Bio = table.Column<string>(type: "TEXT", maxLength: 900, nullable: true),
                    JoinedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsFeatured = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneralAssemblyMembers", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GeneralAssemblyMembers");
        }
    }
}
