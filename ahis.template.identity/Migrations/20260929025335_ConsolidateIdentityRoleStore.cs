using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ahis.template.identity.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateIdentityRoleStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[IdentityRoles]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[IdentityRoles]
                    (
                        [Id] nvarchar(450) NOT NULL,
                        [Name] nvarchar(max) NULL,
                        [NormalizedName] nvarchar(max) NULL,
                        [ConcurrencyStamp] nvarchar(max) NULL,
                        CONSTRAINT [PK_IdentityRoles] PRIMARY KEY ([Id])
                    );
                END;

                IF EXISTS (SELECT 1 FROM [dbo].[IdentityRoles])
                    THROW 51000, 'IdentityRoles contains rows; reconcile them before consolidating the role store.', 1;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_IdentityRoleClaims_AspNetRoles_RoleId",
                table: "IdentityRoleClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_IdentityUserRoles_AspNetRoles_RoleId",
                table: "IdentityUserRoles");

            migrationBuilder.DropTable(
                name: "IdentityRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetRoles",
                table: "AspNetRoles");

            migrationBuilder.RenameTable(
                name: "AspNetRoles",
                newName: "IdentityRole");

            migrationBuilder.AddPrimaryKey(
                name: "PK_IdentityRole",
                table: "IdentityRole",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_IdentityRoleClaims_IdentityRole_RoleId",
                table: "IdentityRoleClaims",
                column: "RoleId",
                principalTable: "IdentityRole",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IdentityUserRoles_IdentityRole_RoleId",
                table: "IdentityUserRoles",
                column: "RoleId",
                principalTable: "IdentityRole",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IdentityRoleClaims_IdentityRole_RoleId",
                table: "IdentityRoleClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_IdentityUserRoles_IdentityRole_RoleId",
                table: "IdentityUserRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_IdentityRole",
                table: "IdentityRole");

            migrationBuilder.RenameTable(
                name: "IdentityRole",
                newName: "AspNetRoles");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetRoles",
                table: "AspNetRoles",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "IdentityRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityRoles", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_IdentityRoleClaims_AspNetRoles_RoleId",
                table: "IdentityRoleClaims",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IdentityUserRoles_AspNetRoles_RoleId",
                table: "IdentityUserRoles",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
