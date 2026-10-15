using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ahis.template.identity.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceRefreshTokensWithHashedSessionModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Legacy values are raw refresh tokens and cannot be safely migrated to
            // the new hash-only model. Clearing them forces a new login after deploy.
            migrationBuilder.Sql("DELETE FROM [RefreshTokens];");

            migrationBuilder.DropColumn(
                name: "Token",
                table: "RefreshTokens");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUsedAt",
                table: "RefreshTokens",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentTokenId",
                table: "RefreshTokens",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SessionId",
                table: "RefreshTokens",
                type: "int",
                nullable: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "TokenHash",
                table: "RefreshTokens",
                type: "binary(32)",
                nullable: false);

            migrationBuilder.CreateTable(
                name: "RefreshSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshSessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ParentTokenId",
                table: "RefreshTokens",
                column: "ParentTokenId",
                unique: true,
                filter: "[ParentTokenId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_SessionId_IsRevoked_ExpiresAt",
                table: "RefreshTokens",
                columns: new[] { "SessionId", "IsRevoked", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshSessions_PublicId",
                table: "RefreshSessions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshSessions_UserId_IsRevoked_ExpiresAt",
                table: "RefreshSessions",
                columns: new[] { "UserId", "IsRevoked", "ExpiresAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_RefreshSessions_SessionId",
                table: "RefreshTokens",
                column: "SessionId",
                principalTable: "RefreshSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_RefreshTokens_ParentTokenId",
                table: "RefreshTokens",
                column: "ParentTokenId",
                principalTable: "RefreshTokens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_RefreshSessions_SessionId",
                table: "RefreshTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_RefreshTokens_ParentTokenId",
                table: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "RefreshSessions");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_ParentTokenId",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_SessionId_IsRevoked_ExpiresAt",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "LastUsedAt",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "ParentTokenId",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "TokenHash",
                table: "RefreshTokens");

            migrationBuilder.AddColumn<string>(
                name: "Token",
                table: "RefreshTokens",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");
        }
    }
}
