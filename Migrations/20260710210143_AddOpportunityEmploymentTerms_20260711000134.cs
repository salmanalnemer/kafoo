using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kafo.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddOpportunityEmploymentTerms_20260711000134 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnnualLeaveDays",
                table: "OpportunityRequests",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SalaryAmount",
                table: "OpportunityRequests",
                type: "TEXT",
                maxLength: 80,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnnualLeaveDays",
                table: "OpportunityRequests");

            migrationBuilder.DropColumn(
                name: "SalaryAmount",
                table: "OpportunityRequests");
        }
    }
}
