using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kafo.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddOpportunityEmploymentFields_20260711_001147 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "SalaryAmount",
                table: "OpportunityRequests",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldMaxLength: 80);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "SalaryAmount",
                table: "OpportunityRequests",
                type: "TEXT",
                maxLength: 80,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
