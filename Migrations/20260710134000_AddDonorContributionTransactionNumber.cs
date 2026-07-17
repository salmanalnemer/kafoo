using Kafo.Web.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kafo.Web.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260710134000_AddDonorContributionTransactionNumber")]
    public partial class AddDonorContributionTransactionNumber : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TransactionNumber",
                table: "DonorContributions",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TransactionNumber",
                table: "DonorContributions");
        }
    }
}
