using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ahis.template.identity.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityRoleBootstrapState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IdentityRoleBootstrapState",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    TargetUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityRoleBootstrapState", x => x.Id);
                    table.CheckConstraint("CK_IdentityRoleBootstrapState_Singleton", "[Id] = 1");
                    table.ForeignKey(
                        name: "FK_IdentityRoleBootstrapState_IdentityUsers_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "IdentityUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IdentityRoleBootstrapState_TargetUserId",
                table: "IdentityRoleBootstrapState",
                column: "TargetUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdentityRoleBootstrapState");
        }
    }
}
