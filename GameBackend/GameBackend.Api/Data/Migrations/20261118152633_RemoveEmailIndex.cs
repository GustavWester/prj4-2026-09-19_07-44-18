using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameBackend.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEmailIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Players_email",
                table: "Players");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Players_email",
                table: "Players",
                column: "email",
                unique: true,
                filter: "[email] IS NOT NULL");
        }
    }
}
