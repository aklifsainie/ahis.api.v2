using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ahis.template.identity.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenSecurityVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SecurityVersion",
                table: "RefreshTokens",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SecurityVersion",
                table: "RefreshTokens");
        }
    }
}
