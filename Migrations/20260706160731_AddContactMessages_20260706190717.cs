using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kafo.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddContactMessages_20260706190717 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContactMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 220, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 180, nullable: true),
                    MessageType = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    MessageBody = table.Column<string>(type: "TEXT", maxLength: 2500, nullable: false),
                    AttachmentPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    AdminNotes = table.Column<string>(type: "TEXT", maxLength: 1500, nullable: true),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactMessages", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactMessages");
        }
    }
}
