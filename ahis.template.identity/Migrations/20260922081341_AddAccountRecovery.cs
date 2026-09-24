using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ahis.template.identity.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountRecoveryChallenges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ChallengeHash = table.Column<byte[]>(type: "binary(32)", nullable: false),
                    SecurityVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountRecoveryChallenges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccountRecoveryThrottles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubjectHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    StartWindowStartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartCount = table.Column<int>(type: "int", nullable: false),
                    CompletionWindowStartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FailedCompletionCount = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountRecoveryThrottles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountRecoveryChallenges_ChallengeHash",
                table: "AccountRecoveryChallenges",
                column: "ChallengeHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountRecoveryChallenges_UserId_ConsumedAt_ExpiresAt",
                table: "AccountRecoveryChallenges",
                columns: new[] { "UserId", "ConsumedAt", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountRecoveryThrottles_SubjectHash",
                table: "AccountRecoveryThrottles",
                column: "SubjectHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountRecoveryChallenges");

            migrationBuilder.DropTable(
                name: "AccountRecoveryThrottles");
        }
    }
}
