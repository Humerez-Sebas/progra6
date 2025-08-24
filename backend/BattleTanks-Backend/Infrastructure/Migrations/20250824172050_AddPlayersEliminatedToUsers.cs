using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddPlayersEliminatedToUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ✅ Crear la columna si no existe (idempotente)
            migrationBuilder.Sql(@"
                ALTER TABLE users
                ADD COLUMN IF NOT EXISTS ""PlayersEliminated"" integer NOT NULL DEFAULT 0;
            ");

            // ❌ Eliminar Health si existe (idempotente)
            migrationBuilder.Sql(@"
                ALTER TABLE players
                DROP COLUMN IF EXISTS ""Health"";
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revertir: quitar PlayersEliminated si existe
            migrationBuilder.Sql(@"
                ALTER TABLE users
                DROP COLUMN IF EXISTS ""PlayersEliminated"";
            ");

            // Revertir: restaurar Health si no existe
            migrationBuilder.Sql(@"
                ALTER TABLE players
                ADD COLUMN IF NOT EXISTS ""Health"" integer NOT NULL DEFAULT 0;
            ");
        }
    }
}