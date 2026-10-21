using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GerenciadorContaFamiliar.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarConviteContaConjunta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConvitesContasConjuntas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContaConjuntaId = table.Column<int>(type: "integer", nullable: false),
                    ConvidadoId = table.Column<int>(type: "integer", nullable: false),
                    ConvidadoPorId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RespondidoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConvitesContasConjuntas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Convites_ContasConjuntas_ContaConjuntaId",
                        column: x => x.ContaConjuntaId,
                        principalTable: "ContasConjuntas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Convites_Membros_ConvidadoId",
                        column: x => x.ConvidadoId,
                        principalTable: "Membros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Convites_Membros_ConvidadoPorId",
                        column: x => x.ConvidadoPorId,
                        principalTable: "Membros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Convites_ContaConjunta_Convidado_Pendente",
                table: "ConvitesContasConjuntas",
                columns: new[] { "ContaConjuntaId", "ConvidadoId" },
                unique: true,
                filter: "\"Status\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Convites_ConvidadoId",
                table: "ConvitesContasConjuntas",
                column: "ConvidadoId");

            migrationBuilder.CreateIndex(
                name: "IX_ConvitesContasConjuntas_ConvidadoPorId",
                table: "ConvitesContasConjuntas",
                column: "ConvidadoPorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConvitesContasConjuntas");
        }
    }
}
