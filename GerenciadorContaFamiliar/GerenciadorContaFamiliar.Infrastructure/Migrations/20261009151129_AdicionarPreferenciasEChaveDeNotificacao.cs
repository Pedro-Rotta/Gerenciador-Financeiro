using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GerenciadorContaFamiliar.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarPreferenciasEChaveDeNotificacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Chave",
                table: "Notificacoes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PreferenciasNotificacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MembroId = table.Column<int>(type: "integer", nullable: false),
                    LancamentosOutros = table.Column<bool>(type: "boolean", nullable: false),
                    Vencimento = table.Column<bool>(type: "boolean", nullable: false),
                    DiasAntesVencimento = table.Column<int>(type: "integer", nullable: false),
                    Atrasada = table.Column<bool>(type: "boolean", nullable: false),
                    SaldoBaixo = table.Column<bool>(type: "boolean", nullable: false),
                    ValorSaldoBaixo = table.Column<decimal>(type: "numeric", nullable: true),
                    GastoAcimaNormal = table.Column<bool>(type: "boolean", nullable: false),
                    MetaProgresso = table.Column<bool>(type: "boolean", nullable: false),
                    MetaPrazo = table.Column<bool>(type: "boolean", nullable: false),
                    HorarioSilencio = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreferenciasNotificacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreferenciasNotificacao_Membros_MembroId",
                        column: x => x.MembroId,
                        principalTable: "Membros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_Membro_Chave_Unico",
                table: "Notificacoes",
                columns: new[] { "MembroId", "Chave" },
                unique: true,
                filter: "\"Chave\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PreferenciasNotificacao_MembroId_Unico",
                table: "PreferenciasNotificacao",
                column: "MembroId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PreferenciasNotificacao");

            migrationBuilder.DropIndex(
                name: "IX_Notificacoes_Membro_Chave_Unico",
                table: "Notificacoes");

            migrationBuilder.DropColumn(
                name: "Chave",
                table: "Notificacoes");
        }
    }
}
