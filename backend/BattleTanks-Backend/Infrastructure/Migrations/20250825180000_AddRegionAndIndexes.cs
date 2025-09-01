using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Migrations
{
    public class AddRegionAndIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "game_sessions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "global");

            migrationBuilder.CreateIndex(
                name: "IX_game_sessions_Status",
                table: "game_sessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_game_sessions_Region",
                table: "game_sessions",
                column: "Region");

            migrationBuilder.CreateIndex(
                name: "IX_game_sessions_Region_Status_IsPublic",
                table: "game_sessions",
                columns: new[] { "Region", "Status", "IsPublic" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_game_sessions_Region_Status_IsPublic",
                table: "game_sessions");

            migrationBuilder.DropIndex(
                name: "IX_game_sessions_Region",
                table: "game_sessions");

            migrationBuilder.DropIndex(
                name: "IX_game_sessions_Status",
                table: "game_sessions");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "game_sessions");
        }
    }
}
