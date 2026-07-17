using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kafo.Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSuccessPartners_20260706074154 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "SuccessPartners",
                type: "TEXT",
                maxLength: 800,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                table: "SuccessPartners",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PartnerType",
                table: "SuccessPartners",
                type: "TEXT",
                maxLength: 180,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "SuccessPartners",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "SuccessPartners");

            migrationBuilder.DropColumn(
                name: "IsFeatured",
                table: "SuccessPartners");

            migrationBuilder.DropColumn(
                name: "PartnerType",
                table: "SuccessPartners");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "SuccessPartners");
        }
    }
}
