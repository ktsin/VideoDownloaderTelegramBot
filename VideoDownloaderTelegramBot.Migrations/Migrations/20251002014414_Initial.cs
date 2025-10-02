using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoDownloaderTelegramBot.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VideoFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SourceUrl = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    TelegramUserId = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoFiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileAccessTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Token = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    VideoFileId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsUsed = table.Column<bool>(type: "INTEGER", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileAccessTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileAccessTokens_VideoFiles_VideoFileId",
                        column: x => x.VideoFileId,
                        principalTable: "VideoFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileAccessTokens_ExpiresAt",
                table: "FileAccessTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_FileAccessTokens_IsUsed",
                table: "FileAccessTokens",
                column: "IsUsed");

            migrationBuilder.CreateIndex(
                name: "IX_FileAccessTokens_Token",
                table: "FileAccessTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileAccessTokens_VideoFileId",
                table: "FileAccessTokens",
                column: "VideoFileId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoFiles_ExpiresAt",
                table: "VideoFiles",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_VideoFiles_FileId",
                table: "VideoFiles",
                column: "FileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VideoFiles_IsDeleted",
                table: "VideoFiles",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_VideoFiles_TelegramUserId",
                table: "VideoFiles",
                column: "TelegramUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileAccessTokens");

            migrationBuilder.DropTable(
                name: "VideoFiles");
        }
    }
}
