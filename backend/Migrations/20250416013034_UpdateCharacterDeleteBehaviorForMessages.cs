using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCharacterDeleteBehaviorForMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_CharacterProfiles_CharacterProfileId",
                table: "ChatMessages");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_CharacterProfiles_CharacterProfileId",
                table: "ChatMessages",
                column: "CharacterProfileId",
                principalTable: "CharacterProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_CharacterProfiles_CharacterProfileId",
                table: "ChatMessages");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_CharacterProfiles_CharacterProfileId",
                table: "ChatMessages",
                column: "CharacterProfileId",
                principalTable: "CharacterProfiles",
                principalColumn: "Id");
        }
    }
}
