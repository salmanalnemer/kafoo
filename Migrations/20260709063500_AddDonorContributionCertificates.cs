using System;
using Kafo.Web.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kafo.Web.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260709063500_AddDonorContributionCertificates")]
    public partial class AddDonorContributionCertificates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SignatureImagePath",
                table: "ExecutiveManagerMessages",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DonorContributionCertificates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DonorContributionId = table.Column<int>(type: "INTEGER", nullable: false),
                    CertificateNumber = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    DonorName = table.Column<string>(type: "TEXT", maxLength: 220, nullable: false),
                    DonorOrganizationName = table.Column<string>(type: "TEXT", maxLength: 220, nullable: true),
                    ContributionTitle = table.Column<string>(type: "TEXT", maxLength: 220, nullable: false),
                    ProgramTitle = table.Column<string>(type: "TEXT", maxLength: 220, nullable: true),
                    ContributionCode = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SpentAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BeneficiariesCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ImpactSummary = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: true),
                    ExecutiveManagerName = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    ExecutiveManagerTitle = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    SignatureImagePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonorContributionCertificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DonorContributionCertificates_DonorContributions_DonorContributionId",
                        column: x => x.DonorContributionId,
                        principalTable: "DonorContributions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DonorContributionCertificates_CertificateNumber",
                table: "DonorContributionCertificates",
                column: "CertificateNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DonorContributionCertificates_DonorContributionId",
                table: "DonorContributionCertificates",
                column: "DonorContributionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DonorContributionCertificates");

            migrationBuilder.DropColumn(
                name: "SignatureImagePath",
                table: "ExecutiveManagerMessages");
        }
    }
}
