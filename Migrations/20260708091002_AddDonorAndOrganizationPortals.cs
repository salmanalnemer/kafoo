using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kafo.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDonorAndOrganizationPortals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DonorAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    DonorType = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    OrganizationName = table.Column<string>(type: "TEXT", maxLength: 180, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 180, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    PasswordSalt = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonorAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 220, nullable: false),
                    LogoPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Activity = table.Column<string>(type: "TEXT", maxLength: 180, nullable: true),
                    City = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    ContactName = table.Column<string>(type: "TEXT", maxLength: 180, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 180, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    PasswordSalt = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DonorContributions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DonorAccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProgramProjectId = table.Column<int>(type: "INTEGER", nullable: true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 220, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    ProgressPercent = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SpentAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BeneficiariesCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ImpactSummary = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    HasSurplus = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSurplusLocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonorContributions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DonorContributions_DonorAccounts_DonorAccountId",
                        column: x => x.DonorAccountId,
                        principalTable: "DonorAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DonorContributions_ProgramProjects_ProgramProjectId",
                        column: x => x.ProgramProjectId,
                        principalTable: "ProgramProjects",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OpportunityRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrganizationAccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    OpportunityType = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 220, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2500, nullable: false),
                    AvailableCount = table.Column<int>(type: "INTEGER", nullable: false),
                    City = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    WorkLocation = table.Column<string>(type: "TEXT", maxLength: 220, nullable: true),
                    Qualifications = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: true),
                    Skills = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: true),
                    SuitableDisabilityTypes = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: true),
                    WorkNature = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    WorkHours = table.Column<string>(type: "TEXT", maxLength: 180, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpportunityRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpportunityRequests_OrganizationAccounts_OrganizationAccountId",
                        column: x => x.OrganizationAccountId,
                        principalTable: "OrganizationAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DonorContributionUpdates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DonorContributionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 220, nullable: false),
                    Details = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: true),
                    ProgressPercent = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonorContributionUpdates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DonorContributionUpdates_DonorContributions_DonorContributionId",
                        column: x => x.DonorContributionId,
                        principalTable: "DonorContributions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DonorNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DonorAccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    DonorContributionId = table.Column<int>(type: "INTEGER", nullable: true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 220, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: false),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    SentBySms = table.Column<bool>(type: "INTEGER", nullable: false),
                    SentByEmail = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonorNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DonorNotifications_DonorAccounts_DonorAccountId",
                        column: x => x.DonorAccountId,
                        principalTable: "DonorAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DonorNotifications_DonorContributions_DonorContributionId",
                        column: x => x.DonorContributionId,
                        principalTable: "DonorContributions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DonorReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DonorContributionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 220, nullable: false),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: true),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ReportType = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    ReportDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonorReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DonorReports_DonorContributions_DonorContributionId",
                        column: x => x.DonorContributionId,
                        principalTable: "DonorContributions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DonorSurplusDecisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DonorContributionId = table.Column<int>(type: "INTEGER", nullable: false),
                    SurplusAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DecisionType = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonorSurplusDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DonorSurplusDecisions_DonorContributions_DonorContributionId",
                        column: x => x.DonorContributionId,
                        principalTable: "DonorContributions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OpportunityCandidates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OpportunityRequestId = table.Column<int>(type: "INTEGER", nullable: false),
                    CandidateName = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    CvFilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Qualifications = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: true),
                    Skills = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: true),
                    OrganizationNotes = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpportunityCandidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpportunityCandidates_OpportunityRequests_OpportunityRequestId",
                        column: x => x.OpportunityRequestId,
                        principalTable: "OpportunityRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationEvaluations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrganizationAccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    OpportunityRequestId = table.Column<int>(type: "INTEGER", nullable: true),
                    CandidateQualityRate = table.Column<int>(type: "INTEGER", nullable: false),
                    ServiceRate = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationEvaluations_OpportunityRequests_OpportunityRequestId",
                        column: x => x.OpportunityRequestId,
                        principalTable: "OpportunityRequests",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrganizationEvaluations_OrganizationAccounts_OrganizationAccountId",
                        column: x => x.OrganizationAccountId,
                        principalTable: "OrganizationAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrganizationAccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    OpportunityRequestId = table.Column<int>(type: "INTEGER", nullable: true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 220, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: false),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    SentBySms = table.Column<bool>(type: "INTEGER", nullable: false),
                    SentByEmail = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationNotifications_OpportunityRequests_OpportunityRequestId",
                        column: x => x.OpportunityRequestId,
                        principalTable: "OpportunityRequests",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrganizationNotifications_OrganizationAccounts_OrganizationAccountId",
                        column: x => x.OrganizationAccountId,
                        principalTable: "OrganizationAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DonorAccounts_Email",
                table: "DonorAccounts",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DonorContributions_DonorAccountId",
                table: "DonorContributions",
                column: "DonorAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_DonorContributions_ProgramProjectId",
                table: "DonorContributions",
                column: "ProgramProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_DonorContributionUpdates_DonorContributionId",
                table: "DonorContributionUpdates",
                column: "DonorContributionId");

            migrationBuilder.CreateIndex(
                name: "IX_DonorNotifications_DonorAccountId",
                table: "DonorNotifications",
                column: "DonorAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_DonorNotifications_DonorContributionId",
                table: "DonorNotifications",
                column: "DonorContributionId");

            migrationBuilder.CreateIndex(
                name: "IX_DonorReports_DonorContributionId",
                table: "DonorReports",
                column: "DonorContributionId");

            migrationBuilder.CreateIndex(
                name: "IX_DonorSurplusDecisions_DonorContributionId",
                table: "DonorSurplusDecisions",
                column: "DonorContributionId");

            migrationBuilder.CreateIndex(
                name: "IX_OpportunityCandidates_OpportunityRequestId",
                table: "OpportunityCandidates",
                column: "OpportunityRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_OpportunityRequests_OrganizationAccountId",
                table: "OpportunityRequests",
                column: "OrganizationAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationAccounts_Email",
                table: "OrganizationAccounts",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationEvaluations_OpportunityRequestId",
                table: "OrganizationEvaluations",
                column: "OpportunityRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationEvaluations_OrganizationAccountId",
                table: "OrganizationEvaluations",
                column: "OrganizationAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationNotifications_OpportunityRequestId",
                table: "OrganizationNotifications",
                column: "OpportunityRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationNotifications_OrganizationAccountId",
                table: "OrganizationNotifications",
                column: "OrganizationAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DonorContributionUpdates");

            migrationBuilder.DropTable(
                name: "DonorNotifications");

            migrationBuilder.DropTable(
                name: "DonorReports");

            migrationBuilder.DropTable(
                name: "DonorSurplusDecisions");

            migrationBuilder.DropTable(
                name: "OpportunityCandidates");

            migrationBuilder.DropTable(
                name: "OrganizationEvaluations");

            migrationBuilder.DropTable(
                name: "OrganizationNotifications");

            migrationBuilder.DropTable(
                name: "DonorContributions");

            migrationBuilder.DropTable(
                name: "OpportunityRequests");

            migrationBuilder.DropTable(
                name: "DonorAccounts");

            migrationBuilder.DropTable(
                name: "OrganizationAccounts");
        }
    }
}
