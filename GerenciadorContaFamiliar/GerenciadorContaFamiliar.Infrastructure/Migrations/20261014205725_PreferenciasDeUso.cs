using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GerenciadorContaFamiliar.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PreferenciasDeUso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PreferenciasUso",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MembroId = table.Column<int>(type: "integer", nullable: false),
                    SemanaComecaNoDomingo = table.Column<bool>(type: "boolean", nullable: false),
                    ContaPadrao = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    PeriodoRelatorios = table.Column<int>(type: "integer", nullable: false),
                    OcultarValores = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreferenciasUso", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreferenciasUso_Membros_MembroId",
                        column: x => x.MembroId,
                        principalTable: "Membros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PreferenciasUso_MembroId_Unico",
                table: "PreferenciasUso",
                column: "MembroId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PreferenciasUso");
        }
    }
}
