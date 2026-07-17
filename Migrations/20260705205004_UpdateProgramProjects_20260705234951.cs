using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kafo.Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProgramProjects_20260705234951 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ProgramProjects",
                type: "TEXT",
                maxLength: 1200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 900,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "ProgramProjects",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 80,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BeneficiariesCount",
                table: "ProgramProjects",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ProgramProjects",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BeneficiariesCount",
                table: "ProgramProjects");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ProgramProjects");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ProgramProjects",
                type: "TEXT",
                maxLength: 900,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 1200);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "ProgramProjects",
                type: "TEXT",
                maxLength: 80,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 100);
        }
    }
}
