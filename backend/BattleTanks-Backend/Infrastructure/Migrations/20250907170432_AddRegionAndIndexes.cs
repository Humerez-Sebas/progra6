using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRegionAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "game_sessions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "global"); // <-- antes era ""

            // Si ya había filas creadas antes de esta migración, asegúrate de backfill:
            migrationBuilder.Sql(@"UPDATE game_sessions SET ""Region"" = 'global' WHERE ""Region"" = '' OR ""Region"" IS NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_game_sessions_Region",
                table: "game_sessions",
                column: "Region");

            migrationBuilder.CreateIndex(
                name: "IX_game_sessions_Region_Status_IsPublic",
                table: "game_sessions",
                columns: new[] { "Region", "Status", "IsPublic" });

            migrationBuilder.CreateIndex(
                name: "IX_game_sessions_Status",
                table: "game_sessions",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_game_sessions_Region",
                table: "game_sessions");

            migrationBuilder.DropIndex(
                name: "IX_game_sessions_Region_Status_IsPublic",
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
