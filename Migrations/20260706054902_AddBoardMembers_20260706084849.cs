using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kafo.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddBoardMembers_20260706084849 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BoardMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 220, nullable: false),
                    PositionTitle = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    MembershipRole = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    BoardTerm = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    TermStartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TermEndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 180, nullable: true),
                    PhotoPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Bio = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IsChairman = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsFeatured = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardMembers", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoardMembers");
        }
    }
}
