using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kafo.Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdateServiceLinksTargetType_20260706003114 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InternalPath",
                table: "ServiceLinks",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkType",
                table: "ServiceLinks",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InternalPath",
                table: "ServiceLinks");

            migrationBuilder.DropColumn(
                name: "LinkType",
                table: "ServiceLinks");
        }
    }
}
