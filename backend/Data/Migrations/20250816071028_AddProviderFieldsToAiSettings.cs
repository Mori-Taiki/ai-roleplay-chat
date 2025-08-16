using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderFieldsToAiSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChatGenerationProvider",
                table: "AiGenerationSettings",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageGenerationProvider",
                table: "AiGenerationSettings",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagePromptGenerationProvider",
                table: "AiGenerationSettings",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SettingsType",
                table: "AiGenerationSettings",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChatGenerationProvider",
                table: "AiGenerationSettings");

            migrationBuilder.DropColumn(
                name: "ImageGenerationProvider",
                table: "AiGenerationSettings");

            migrationBuilder.DropColumn(
                name: "ImagePromptGenerationProvider",
                table: "AiGenerationSettings");

            migrationBuilder.DropColumn(
                name: "SettingsType",
                table: "AiGenerationSettings");
        }
    }
}
