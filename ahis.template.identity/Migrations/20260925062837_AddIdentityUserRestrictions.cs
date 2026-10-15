using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ahis.template.identity.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityUserRestrictions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IdentityUserRestrictions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Origin = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PlacedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    EndedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReviewDecision = table.Column<int>(type: "int", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    EvidenceReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InternalReasonCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityUserRestrictions", x => x.Id);
                    table.CheckConstraint("CK_IdentityUserRestrictions_CategoryExpiry", "([Category] = 1 AND [ExpiresAtUtc] IS NOT NULL) OR ([Category] IN (2, 3) AND [ExpiresAtUtc] IS NULL)");
                    table.CheckConstraint("CK_IdentityUserRestrictions_EndConsistency", "[EndedAtUtc] IS NULL OR [EndedAtUtc] >= [StartedAtUtc]");
                    table.ForeignKey(
                        name: "FK_IdentityUserRestrictions_IdentityUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "IdentityUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IdentityUserRestrictions_Category_EndedAtUtc",
                table: "IdentityUserRestrictions",
                columns: new[] { "Category", "EndedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_IdentityUserRestrictions_UserId",
                table: "IdentityUserRestrictions",
                column: "UserId",
                unique: true,
                filter: "[EndedAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityUserRestrictions_UserId_StartedAtUtc",
                table: "IdentityUserRestrictions",
                columns: new[] { "UserId", "StartedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [IdentityUserRestrictions])
                    THROW 51000, 'IdentityUserRestrictions contains provenance data and cannot be dropped.', 1;
                """);
            migrationBuilder.DropTable(
                name: "IdentityUserRestrictions");
        }
    }
}
