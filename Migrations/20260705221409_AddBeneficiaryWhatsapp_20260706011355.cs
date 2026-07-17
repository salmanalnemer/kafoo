using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kafo.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddBeneficiaryWhatsapp_20260706011355 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowWhatsAppButton",
                table: "BeneficiaryDataUpdatePages",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppButtonText",
                table: "BeneficiaryDataUpdatePages",
                type: "TEXT",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppMessage",
                table: "BeneficiaryDataUpdatePages",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppNumber",
                table: "BeneficiaryDataUpdatePages",
                type: "TEXT",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowWhatsAppButton",
                table: "BeneficiaryDataUpdatePages");

            migrationBuilder.DropColumn(
                name: "WhatsAppButtonText",
                table: "BeneficiaryDataUpdatePages");

            migrationBuilder.DropColumn(
                name: "WhatsAppMessage",
                table: "BeneficiaryDataUpdatePages");

            migrationBuilder.DropColumn(
                name: "WhatsAppNumber",
                table: "BeneficiaryDataUpdatePages");
        }
    }
}
