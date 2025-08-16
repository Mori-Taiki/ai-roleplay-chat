using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterModelSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
        }
    }
}
