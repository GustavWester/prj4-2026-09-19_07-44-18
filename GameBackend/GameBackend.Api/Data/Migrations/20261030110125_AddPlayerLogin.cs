using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameBackend.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerLogin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Players_user_id_game_id",
                table: "Players");

            migrationBuilder.AlterColumn<int>(
                name: "game_id",
                table: "Players",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "Players",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "password_hash",
                table: "Players",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_email",
                table: "Players",
                column: "email",
                unique: true,
                filter: "[email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Players_user_id_game_id",
                table: "Players",
                columns: new[] { "user_id", "game_id" },
                unique: true,
                filter: "[game_id] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Players_email",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Players_user_id_game_id",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "email",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "password_hash",
                table: "Players");

            migrationBuilder.AlterColumn<int>(
                name: "game_id",
                table: "Players",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_user_id_game_id",
                table: "Players",
                columns: new[] { "user_id", "game_id" },
                unique: true);
        }
    }
}
