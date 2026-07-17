using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kafo.Web.Migrations
{
    /// <inheritdoc />
    public partial class SyncDonorContributionModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Migration intentionally left empty.
            // The TransactionNumber column already exists in the SQLite database.
            // This migration only synchronizes the EF Core model snapshot.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No rollback operation is required.
        }
    }
}