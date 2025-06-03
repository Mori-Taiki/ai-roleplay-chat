using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateSqlite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CharacterProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Personality = table.Column<string>(type: "TEXT", nullable: true),
                    Tone = table.Column<string>(type: "TEXT", nullable: true),
                    Backstory = table.Column<string>(type: "TEXT", nullable: true),
                    SystemPrompt = table.Column<string>(type: "TEXT", nullable: true),
                    ExampleDialogue = table.Column<string>(type: "JSON", nullable: true),
                    AvatarImageUrl = table.Column<string>(type: "TEXT", maxLength: 2083, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSystemPromptCustomized = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    B2cObjectId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "DATETIME(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatSessions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    CharacterProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    StartTime = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    EndTime = table.Column<DateTime>(type: "DATETIME(6)", nullable: true),
                    Metadata = table.Column<string>(type: "JSON", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "DATETIME(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatSessions_CharacterProfiles_CharacterProfileId",
                        column: x => x.CharacterProfileId,
                        principalTable: "CharacterProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    CharacterProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Sender = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    ImageUrl = table.Column<string>(type: "MEDIUMTEXT", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "DATETIME(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_CharacterProfiles_CharacterProfileId",
                        column: x => x.CharacterProfileId,
                        principalTable: "CharacterProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatMessages_ChatSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "ChatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_CharacterProfileId",
                table: "ChatMessages",
                column: "CharacterProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SessionId_Timestamp",
                table: "ChatMessages",
                columns: new[] { "SessionId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_CharacterProfileId",
                table: "ChatSessions",
                column: "CharacterProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_User_Character_StartTime",
                table: "ChatSessions",
                columns: new[] { "UserId", "CharacterProfileId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_B2cObjectId",
                table: "Users",
                column: "B2cObjectId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "ChatSessions");

            migrationBuilder.DropTable(
                name: "CharacterProfiles");
        }
    }
}
