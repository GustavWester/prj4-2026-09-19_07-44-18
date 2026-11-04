using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameBackend.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemovePlayerPasswordHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seed-konti (de eneste med password_hash) fjernes, så de ikke kolliderer
            // med rigtige website-brugere på user_id/email
            migrationBuilder.Sql("DELETE FROM Players WHERE password_hash IS NOT NULL;");

            migrationBuilder.DropColumn(
                name: "password_hash",
                table: "Players");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "password_hash",
                table: "Players",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
