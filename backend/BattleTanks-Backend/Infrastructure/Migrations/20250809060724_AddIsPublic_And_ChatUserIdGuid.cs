using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPublic_And_ChatUserIdGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Limpiar relaciones/columnas fantasma creadas por configuraciones previas
            migrationBuilder.DropForeignKey(
                name: "FK_players_game_sessions_GameSessionId1",
                table: "players");

            migrationBuilder.DropForeignKey(
                name: "FK_scores_game_sessions_GameSessionId1",
                table: "scores");

            migrationBuilder.DropIndex(
                name: "IX_scores_GameSessionId1",
                table: "scores");

            migrationBuilder.DropIndex(
                name: "IX_players_GameSessionId1",
                table: "players");

            migrationBuilder.DropColumn(
                name: "GameSessionId1",
                table: "scores");

            migrationBuilder.DropColumn(
                name: "GameSessionId1",
                table: "players");

            // Nueva columna: game_sessions.IsPublic
            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "game_sessions",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            // --- Cambio de tipo chat_messages.UserId (text -> uuid) ---

            // 1) Normalizar valores no casteables a Guid.Empty
            migrationBuilder.Sql(@"
                UPDATE chat_messages
                SET ""UserId"" = '00000000-0000-0000-0000-000000000000'
                WHERE ""UserId"" IS NULL OR ""UserId"" = '' OR ""UserId"" = 'SYSTEM';
            ");

            // 2) Alter column con USING ::uuid
            migrationBuilder.Sql(@"
                ALTER TABLE chat_messages
                ALTER COLUMN ""UserId"" TYPE uuid
                USING ""UserId""::uuid;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revertir IsPublic
            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "game_sessions");

            // Restaurar columnas fantasma (solo si realmente necesitas volver atrás)
            migrationBuilder.AddColumn<Guid>(
                name: "GameSessionId1",
                table: "scores",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GameSessionId1",
                table: "players",
                type: "uuid",
                nullable: true);

            // Revertir chat_messages.UserId a texto
            migrationBuilder.Sql(@"
                ALTER TABLE chat_messages
                ALTER COLUMN ""UserId"" TYPE character varying(50);
            ");

            // Opcional: volver Guid.Empty -> 'SYSTEM'
            migrationBuilder.Sql(@"
                UPDATE chat_messages
                SET ""UserId"" = 'SYSTEM'
                WHERE ""UserId"" = '00000000-0000-0000-0000-000000000000';
            ");

            migrationBuilder.CreateIndex(
                name: "IX_scores_GameSessionId1",
                table: "scores",
                column: "GameSessionId1");

            migrationBuilder.CreateIndex(
                name: "IX_players_GameSessionId1",
                table: "players",
                column: "GameSessionId1");

            migrationBuilder.AddForeignKey(
                name: "FK_players_game_sessions_GameSessionId1",
                table: "players",
                column: "GameSessionId1",
                principalTable: "game_sessions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_scores_game_sessions_GameSessionId1",
                table: "scores",
                column: "GameSessionId1",
                principalTable: "game_sessions",
                principalColumn: "Id");
        }
    }
}
