using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GerenciadorContaFamiliar.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarRelacionamentosEAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notificacoes_Membros_MembroId",
                table: "Notificacoes");

            migrationBuilder.DropForeignKey(
                name: "FK_SubCategorias_Categorias_CategoriaId",
                table: "SubCategorias");

            migrationBuilder.DropForeignKey(
                name: "FK_Transacoes_Membros_MembroId",
                table: "Transacoes");

            migrationBuilder.CreateIndex(
                name: "IX_Metas_MembroId",
                table: "Metas",
                column: "MembroId");

            migrationBuilder.CreateIndex(
                name: "IX_LogsAuditoria_IdMembro",
                table: "LogsAuditoria",
                column: "IdMembro");

            migrationBuilder.AddForeignKey(
                name: "FK_LogsAuditoria_Membros_IdMembro",
                table: "LogsAuditoria",
                column: "IdMembro",
                principalTable: "Membros",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Metas_Membros_MembroId",
                table: "Metas",
                column: "MembroId",
                principalTable: "Membros",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Notificacoes_Membros_MembroId",
                table: "Notificacoes",
                column: "MembroId",
                principalTable: "Membros",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SubCategorias_Categorias_CategoriaId",
                table: "SubCategorias",
                column: "CategoriaId",
                principalTable: "Categorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transacoes_Membros_MembroId",
                table: "Transacoes",
                column: "MembroId",
                principalTable: "Membros",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LogsAuditoria_Membros_IdMembro",
                table: "LogsAuditoria");

            migrationBuilder.DropForeignKey(
                name: "FK_Metas_Membros_MembroId",
                table: "Metas");

            migrationBuilder.DropForeignKey(
                name: "FK_Notificacoes_Membros_MembroId",
                table: "Notificacoes");

            migrationBuilder.DropForeignKey(
                name: "FK_SubCategorias_Categorias_CategoriaId",
                table: "SubCategorias");

            migrationBuilder.DropForeignKey(
                name: "FK_Transacoes_Membros_MembroId",
                table: "Transacoes");

            migrationBuilder.DropIndex(
                name: "IX_Metas_MembroId",
                table: "Metas");

            migrationBuilder.DropIndex(
                name: "IX_LogsAuditoria_IdMembro",
                table: "LogsAuditoria");

            migrationBuilder.AddForeignKey(
                name: "FK_Notificacoes_Membros_MembroId",
                table: "Notificacoes",
                column: "MembroId",
                principalTable: "Membros",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SubCategorias_Categorias_CategoriaId",
                table: "SubCategorias",
                column: "CategoriaId",
                principalTable: "Categorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transacoes_Membros_MembroId",
                table: "Transacoes",
                column: "MembroId",
                principalTable: "Membros",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
