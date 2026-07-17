using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kafo.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminUsersAndPermissions_20260706194732 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 180, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    PasswordSalt = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsSuperAdmin = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdminPagePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AdminUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    SectionName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    PageName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    PagePath = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    CanAccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminPagePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminPagePermissions_AdminUsers_AdminUserId",
                        column: x => x.AdminUserId,
                        principalTable: "AdminUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminPagePermissions_AdminUserId",
                table: "AdminPagePermissions",
                column: "AdminUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminPagePermissions");

            migrationBuilder.DropTable(
                name: "AdminUsers");
        }
    }
}
