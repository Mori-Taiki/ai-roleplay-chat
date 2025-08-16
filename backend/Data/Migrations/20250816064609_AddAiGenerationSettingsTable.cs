using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAiGenerationSettingsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageModelId",
                table: "CharacterProfiles");

            migrationBuilder.DropColumn(
                name: "ImageModelProvider",
                table: "CharacterProfiles");

            migrationBuilder.DropColumn(
                name: "TextModelId",
                table: "CharacterProfiles");

            migrationBuilder.DropColumn(
                name: "TextModelProvider",
                table: "CharacterProfiles");

            migrationBuilder.AddColumn<int>(
                name: "AiSettingsId",
                table: "Users",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AiSettingsId",
                table: "CharacterProfiles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AiGenerationSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChatGenerationModel = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ImagePromptGenerationModel = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ImageGenerationModel = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ImageGenerationPromptInstruction = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiGenerationSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_AiSettingsId",
                table: "Users",
                column: "AiSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterProfiles_AiSettingsId",
                table: "CharacterProfiles",
                column: "AiSettingsId");

            migrationBuilder.AddForeignKey(
                name: "FK_CharacterProfiles_AiGenerationSettings_AiSettingsId",
                table: "CharacterProfiles",
                column: "AiSettingsId",
                principalTable: "AiGenerationSettings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_AiGenerationSettings_AiSettingsId",
                table: "Users",
                column: "AiSettingsId",
                principalTable: "AiGenerationSettings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CharacterProfiles_AiGenerationSettings_AiSettingsId",
                table: "CharacterProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_AiGenerationSettings_AiSettingsId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "AiGenerationSettings");

            migrationBuilder.DropIndex(
                name: "IX_Users_AiSettingsId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_CharacterProfiles_AiSettingsId",
                table: "CharacterProfiles");

            migrationBuilder.DropColumn(
                name: "AiSettingsId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AiSettingsId",
                table: "CharacterProfiles");

            migrationBuilder.AddColumn<string>(
                name: "ImageModelId",
                table: "CharacterProfiles",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageModelProvider",
                table: "CharacterProfiles",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextModelId",
                table: "CharacterProfiles",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextModelProvider",
                table: "CharacterProfiles",
                type: "TEXT",
                maxLength: 50,
                nullable: true);
        }
    }
}
