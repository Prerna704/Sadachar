using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sadachar.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessibleResourceLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccessibilityNotes",
                table: "LearningResources",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Difficulty",
                table: "LearningResources",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "EstimatedMinutes",
                table: "LearningResources",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUrl",
                table: "LearningResources",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ResourceCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceCategories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceCategories_Name",
                table: "ResourceCategories",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResourceCategories");

            migrationBuilder.DropColumn(
                name: "AccessibilityNotes",
                table: "LearningResources");

            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "LearningResources");

            migrationBuilder.DropColumn(
                name: "EstimatedMinutes",
                table: "LearningResources");

            migrationBuilder.DropColumn(
                name: "ThumbnailUrl",
                table: "LearningResources");
        }
    }
}
