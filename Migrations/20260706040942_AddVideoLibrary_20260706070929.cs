using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kafo.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoLibrary_20260706070929 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VideoLibraryItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 900, nullable: true),
                    VideoSourceType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    YoutubeUrl = table.Column<string>(type: "TEXT", maxLength: 800, nullable: true),
                    VideoPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ThumbnailPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsFeatured = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoLibraryItems", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VideoLibraryItems");
        }
    }
}
