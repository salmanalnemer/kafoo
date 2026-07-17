using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kafo.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddBankAccounts_20260705234429 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BankName = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    AccountName = table.Column<string>(type: "TEXT", maxLength: 220, nullable: false),
                    Iban = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    AccountNumber = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    LogoPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccounts", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankAccounts");
        }
    }
}
