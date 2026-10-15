using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ahis.template.identity.Migrations
{
    /// <inheritdoc />
    public partial class AddPasskeyCredentialsAndPasswordResetState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MustResetPassword",
                table: "IdentityUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PasskeyCeremonies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChallengeReferenceHash = table.Column<byte[]>(type: "binary(32)", nullable: false),
                    ChallengeHash = table.Column<byte[]>(type: "binary(32)", nullable: false),
                    Purpose = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SecurityVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RpConfigurationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConsumedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RateLimitKeyHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UserRateLimitKeyHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    OptionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProtectedChallenge = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasskeyCeremonies", x => x.Id);
                    table.CheckConstraint("CK_PasskeyCeremonies_Purpose", "[Purpose] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_PasskeyCeremonies_IdentityUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "IdentityUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PasskeyCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CredentialId = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PublicKey = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    SignCount = table.Column<long>(type: "bigint", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUsedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsBackupEligible = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsBackedUp = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasskeyCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasskeyCredentials_IdentityUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "IdentityUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PasskeyCeremonies_ChallengeReferenceHash",
                table: "PasskeyCeremonies",
                column: "ChallengeReferenceHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasskeyCeremonies_Purpose_UserId_ExpiresAtUtc_ConsumedAtUtc",
                table: "PasskeyCeremonies",
                columns: new[] { "Purpose", "UserId", "ExpiresAtUtc", "ConsumedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PasskeyCeremonies_UserId",
                table: "PasskeyCeremonies",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PasskeyCredentials_CredentialId",
                table: "PasskeyCredentials",
                column: "CredentialId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasskeyCredentials_UserId_CreatedAtUtc",
                table: "PasskeyCredentials",
                columns: new[] { "UserId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM [dbo].[PasskeyCeremonies]) OR EXISTS (SELECT 1 FROM [dbo].[PasskeyCredentials])
    THROW 51000, 'Passkey tables contain rows; rollback is unsafe.', 1;
IF EXISTS (SELECT 1 FROM [dbo].[IdentityUsers] WHERE [MustResetPassword] = 1)
    THROW 51001, 'Password reset interventions are active; rollback is unsafe.', 1;");

            migrationBuilder.DropTable(name: "PasskeyCeremonies");
            migrationBuilder.DropTable(name: "PasskeyCredentials");
            migrationBuilder.DropColumn(name: "MustResetPassword", table: "IdentityUsers");
        }
    }
}
