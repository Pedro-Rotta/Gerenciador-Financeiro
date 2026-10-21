using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GerenciadorContaFamiliar.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarRecorrencias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NumeroOcorrencia",
                table: "Transacoes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecorrenciaId",
                table: "Transacoes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Recorrencias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Valor = table.Column<decimal>(type: "numeric", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: true),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Classificacao = table.Column<int>(type: "integer", nullable: false),
                    Periodicidade = table.Column<int>(type: "integer", nullable: false),
                    MembroId = table.Column<int>(type: "integer", nullable: false),
                    ContaId = table.Column<int>(type: "integer", nullable: true),
                    ContaConjuntaId = table.Column<int>(type: "integer", nullable: true),
                    CategoriaId = table.Column<int>(type: "integer", nullable: true),
                    SubCategoriaId = table.Column<int>(type: "integer", nullable: true),
                    DataInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    DiaDoMes = table.Column<int>(type: "integer", nullable: false),
                    ProximaData = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalOcorrencias = table.Column<int>(type: "integer", nullable: true),
                    OcorrenciasGeradas = table.Column<int>(type: "integer", nullable: false),
                    Ativa = table.Column<bool>(type: "boolean", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recorrencias", x => x.Id);
                    table.CheckConstraint("CHK_Recorrencia_UmOuOutroTipoConta", "(\"ContaId\" IS NOT NULL AND \"ContaConjuntaId\" IS NULL) OR (\"ContaId\" IS NULL AND \"ContaConjuntaId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Recorrencias_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recorrencias_ContasConjuntas_ContaConjuntaId",
                        column: x => x.ContaConjuntaId,
                        principalTable: "ContasConjuntas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recorrencias_Contas_ContaId",
                        column: x => x.ContaId,
                        principalTable: "Contas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recorrencias_Membros_MembroId",
                        column: x => x.MembroId,
                        principalTable: "Membros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recorrencias_SubCategorias_SubCategoriaId",
                        column: x => x.SubCategoriaId,
                        principalTable: "SubCategorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transacoes_Recorrencia_Data_Unico",
                table: "Transacoes",
                columns: new[] { "RecorrenciaId", "DataTransacao" },
                unique: true,
                filter: "\"RecorrenciaId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Recorrencias_CategoriaId",
                table: "Recorrencias",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Recorrencias_ContaConjuntaId",
                table: "Recorrencias",
                column: "ContaConjuntaId");

            migrationBuilder.CreateIndex(
                name: "IX_Recorrencias_ContaId",
                table: "Recorrencias",
                column: "ContaId");

            migrationBuilder.CreateIndex(
                name: "IX_Recorrencias_MembroId",
                table: "Recorrencias",
                column: "MembroId");

            migrationBuilder.CreateIndex(
                name: "IX_Recorrencias_SubCategoriaId",
                table: "Recorrencias",
                column: "SubCategoriaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transacoes_Recorrencias_RecorrenciaId",
                table: "Transacoes",
                column: "RecorrenciaId",
                principalTable: "Recorrencias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transacoes_Recorrencias_RecorrenciaId",
                table: "Transacoes");

            migrationBuilder.DropTable(
                name: "Recorrencias");

            migrationBuilder.DropIndex(
                name: "IX_Transacoes_Recorrencia_Data_Unico",
                table: "Transacoes");

            migrationBuilder.DropColumn(
                name: "NumeroOcorrencia",
                table: "Transacoes");

            migrationBuilder.DropColumn(
                name: "RecorrenciaId",
                table: "Transacoes");
        }
    }
}
