using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kafo.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDonorContributionCode20260709081529 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContributionCode",
                table: "DonorContributions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DonorContributions_ContributionCode",
                table: "DonorContributions",
                column: "ContributionCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DonorContributions_ContributionCode",
                table: "DonorContributions");

            migrationBuilder.DropColumn(
                name: "ContributionCode",
                table: "DonorContributions");
        }
    }
}
