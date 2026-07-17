using Kafo.Web.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kafo.Web.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260717010000_SecurityHardening")]
public partial class SecurityHardening : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        AddAccountSecurityColumns(migrationBuilder, "AdminUsers");
        AddAccountSecurityColumns(migrationBuilder, "DonorAccounts");
        AddAccountSecurityColumns(migrationBuilder, "OrganizationAccounts");

        migrationBuilder.CreateTable(
            name: "LoginOtpChallenges",
            columns: table => new
            {
                ChallengeId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                PortalType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                AccountId = table.Column<int>(type: "INTEGER", nullable: false),
                Email = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                DisplayName = table.Column<string>(type: "TEXT", maxLength: 220, nullable: false),
                RememberMe = table.Column<bool>(type: "INTEGER", nullable: false),
                ReturnUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                IpAddress = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                ProtectedCode = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                Attempts = table.Column<int>(type: "INTEGER", nullable: false),
                SendCount = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                LastSentAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UsedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_LoginOtpChallenges", x => x.ChallengeId));

        migrationBuilder.CreateTable(
            name: "PasswordSetupTokens",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                AccountType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                AccountId = table.Column<int>(type: "INTEGER", nullable: false),
                TokenHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UsedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                RequestedByAdminUserId = table.Column<int>(type: "INTEGER", nullable: true),
                IpAddress = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_PasswordSetupTokens", x => x.Id));

        migrationBuilder.CreateTable(
            name: "SystemLogs",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                EventType = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                Severity = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                ActorType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                ActorId = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                IpAddress = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                UserAgent = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                Path = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                Message = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                Success = table.Column<bool>(type: "INTEGER", nullable: false),
                CorrelationId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_SystemLogs", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_LoginOtpChallenges_Email_LastSentAtUtc",
            table: "LoginOtpChallenges",
            columns: new[] { "Email", "LastSentAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_LoginOtpChallenges_PortalType_AccountId_ExpiresAtUtc",
            table: "LoginOtpChallenges",
            columns: new[] { "PortalType", "AccountId", "ExpiresAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_PasswordSetupTokens_AccountType_AccountId_ExpiresAtUtc",
            table: "PasswordSetupTokens",
            columns: new[] { "AccountType", "AccountId", "ExpiresAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_PasswordSetupTokens_TokenHash",
            table: "PasswordSetupTokens",
            column: "TokenHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SystemLogs_CreatedAt",
            table: "SystemLogs",
            column: "CreatedAt");

        migrationBuilder.CreateIndex(
            name: "IX_SystemLogs_EventType_Success",
            table: "SystemLogs",
            columns: new[] { "EventType", "Success" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "LoginOtpChallenges");
        migrationBuilder.DropTable(name: "PasswordSetupTokens");
        migrationBuilder.DropTable(name: "SystemLogs");

        DropAccountSecurityColumns(migrationBuilder, "AdminUsers");
        DropAccountSecurityColumns(migrationBuilder, "DonorAccounts");
        DropAccountSecurityColumns(migrationBuilder, "OrganizationAccounts");
    }

    private static void AddAccountSecurityColumns(MigrationBuilder migrationBuilder, string table)
    {
        migrationBuilder.AddColumn<int>(name: "AccessFailedCount", table: table, type: "INTEGER", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<DateTime?>(name: "LockoutEndUtc", table: table, type: "TEXT", nullable: true);
        migrationBuilder.AddColumn<bool>(name: "MustChangePassword", table: table, type: "INTEGER", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<DateTime?>(name: "PasswordChangedAtUtc", table: table, type: "TEXT", nullable: true);
        migrationBuilder.AddColumn<string>(name: "SecurityStamp", table: table, type: "TEXT", maxLength: 64, nullable: false, defaultValue: "");
    }

    private static void DropAccountSecurityColumns(MigrationBuilder migrationBuilder, string table)
    {
        migrationBuilder.DropColumn(name: "AccessFailedCount", table: table);
        migrationBuilder.DropColumn(name: "LockoutEndUtc", table: table);
        migrationBuilder.DropColumn(name: "MustChangePassword", table: table);
        migrationBuilder.DropColumn(name: "PasswordChangedAtUtc", table: table);
        migrationBuilder.DropColumn(name: "SecurityStamp", table: table);
    }
}
