using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ahis.template.identity.Migrations
{
    /// <inheritdoc />
    public partial class AddFirstSuperadminBootstrapMarker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FirstSuperadminBootstrap",
                columns: table => new
                {
                    Id = table.Column<byte>(type: "tinyint", nullable: false),
                    TargetUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FirstSuperadminBootstrap", x => x.Id);
                    table.CheckConstraint("CK_FirstSuperadminBootstrap_Id", "[Id] = 1");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [dbo].[FirstSuperadminBootstrap])
                    THROW 51000, 'Bootstrap completion marker exists; rollback is unsafe.', 1;
                DROP TABLE [dbo].[FirstSuperadminBootstrap];
                """);
        }
    }
}
