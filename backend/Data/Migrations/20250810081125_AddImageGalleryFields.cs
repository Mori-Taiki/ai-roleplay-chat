using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddImageGalleryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ChatMessages",
                type: "DATETIME(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagePrompt",
                table: "ChatMessages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelId",
                table: "ChatMessages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceName",
                table: "ChatMessages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_User_Character_Session_CreatedAt",
                table: "ChatMessages",
                columns: new[] { "UserId", "CharacterProfileId", "SessionId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_User_Character_Session_CreatedAt",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "ImagePrompt",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "ModelId",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "ServiceName",
                table: "ChatMessages");
        }
    }
}
