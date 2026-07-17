using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kafo.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddInKindDonationRequests_20260706021439 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InKindDonationRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DonorName = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    Mobile = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    City = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    District = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    DonationTypeIds = table.Column<string>(type: "TEXT", nullable: false),
                    DonationTypeNames = table.Column<string>(type: "TEXT", nullable: false),
                    PreferredPickupTime = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InKindDonationRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InKindDonationTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InKindDonationTypes", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InKindDonationRequests");

            migrationBuilder.DropTable(
                name: "InKindDonationTypes");
        }
    }
}
