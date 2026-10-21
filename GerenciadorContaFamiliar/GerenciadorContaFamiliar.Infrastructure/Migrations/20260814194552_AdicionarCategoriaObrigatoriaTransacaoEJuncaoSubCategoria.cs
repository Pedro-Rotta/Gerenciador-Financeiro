using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GerenciadorContaFamiliar.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarCategoriaObrigatoriaTransacaoEJuncaoSubCategoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ─────────────────────────────────────────────────────────────
            // 1) Transacoes.Ativo (soft delete) — default TRUE para não
            //    inativar transações existentes.
            // ─────────────────────────────────────────────────────────────
            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "Transacoes",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            // ─────────────────────────────────────────────────────────────
            // 2) Transacoes.CategoriaId — adicionada como NULLABLE primeiro
            //    para permitir o backfill antes de tornar obrigatória.
            // ─────────────────────────────────────────────────────────────
            migrationBuilder.AddColumn<int>(
                name: "CategoriaId",
                table: "Transacoes",
                type: "integer",
                nullable: true);

            // ─────────────────────────────────────────────────────────────
            // 3) Backfill 1: transações que possuem SubCategoria herdam a
            //    Categoria dela (fonte: SubCategorias.CategoriaId, que será
            //    removida nesta migração — por isso o UPDATE vem antes).
            // ─────────────────────────────────────────────────────────────
            migrationBuilder.Sql("""
                UPDATE "Transacoes" t
                SET "CategoriaId" = s."CategoriaId"
                FROM "SubCategorias" s
                WHERE t."SubCategoriaId" = s."Id"
                  AND t."CategoriaId" IS NULL;
                """);

            // ─────────────────────────────────────────────────────────────
            // 4) Backfill 2: transações órfãs (sem SubCategoria) recebem uma
            //    categoria padrão "Sem Categoria" para não violar a FK.
            // ─────────────────────────────────────────────────────────────
            migrationBuilder.Sql("""
                INSERT INTO "Categorias" ("Nome", "Tipo", "Necessario", "Icone", "Ativo")
                SELECT 'Sem Categoria', 0, false, NULL, true
                WHERE NOT EXISTS (SELECT 1 FROM "Categorias" WHERE "Nome" = 'Sem Categoria');

                UPDATE "Transacoes"
                SET "CategoriaId" = (SELECT "Id" FROM "Categorias" WHERE "Nome" = 'Sem Categoria')
                WHERE "CategoriaId" IS NULL;
                """);

            // ─────────────────────────────────────────────────────────────
            // 5) Transacoes.CategoriaId → torna-se obrigatória.
            // ─────────────────────────────────────────────────────────────
            migrationBuilder.AlterColumn<int>(
                name: "CategoriaId",
                table: "Transacoes",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            // ─────────────────────────────────────────────────────────────
            // 6) SubCategorias deixa de depender de Categoria (1:N removida).
            // ─────────────────────────────────────────────────────────────
            migrationBuilder.DropForeignKey(
                name: "FK_SubCategorias_Categorias_CategoriaId",
                table: "SubCategorias");

            migrationBuilder.DropIndex(
                name: "IX_SubCategorias_CategoriaId",
                table: "SubCategorias");

            migrationBuilder.DropColumn(
                name: "CategoriaId",
                table: "SubCategorias");

            // ─────────────────────────────────────────────────────────────
            // 6b) Ajustes de modelo pendentes desde a última migração:
            //     - Membros.Email / Membros.Telefone tornam-se nullable.
            //     - Categorias.Necessario ganha default true.
            // ─────────────────────────────────────────────────────────────
            migrationBuilder.AlterColumn<string>(
                name: "Telefone",
                table: "Membros",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Membros",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<bool>(
                name: "Necessario",
                table: "Categorias",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            // ─────────────────────────────────────────────────────────────
            // 7) Junção N:N Categoria <-> SubCategoria.
            // ─────────────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "CategoriaSubCategoria",
                columns: table => new
                {
                    CategoriaId = table.Column<int>(type: "integer", nullable: false),
                    SubCategoriaId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriaSubCategoria", x => new { x.CategoriaId, x.SubCategoriaId });
                    table.ForeignKey(
                        name: "FK_CategoriaSubCategoria_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CategoriaSubCategoria_SubCategorias_SubCategoriaId",
                        column: x => x.SubCategoriaId,
                        principalTable: "SubCategorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ─────────────────────────────────────────────────────────────
            // 8) Índices e FK nova de Transacao -> Categoria.
            // ─────────────────────────────────────────────────────────────
            migrationBuilder.CreateIndex(
                name: "IX_Transacoes_CategoriaId",
                table: "Transacoes",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoriaSubCategoria_SubCategoriaId",
                table: "CategoriaSubCategoria",
                column: "SubCategoriaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transacoes_Categorias_CategoriaId",
                table: "Transacoes",
                column: "CategoriaId",
                principalTable: "Categorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // ─────────────────────────────────────────────────────────────
            // 9) Trigger de cascata de soft delete: inativar uma
            //    SubCategoria inativa as Transacoes vinculadas e remove os
            //    vínculos N:N.
            // ─────────────────────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION fn_cascade_subcategoria_inativada()
                RETURNS TRIGGER AS $$
                BEGIN
                    IF (OLD."Ativo" = true AND NEW."Ativo" = false) THEN
                        UPDATE "Transacoes"
                        SET "Ativo" = false
                        WHERE "SubCategoriaId" = NEW."Id" AND "Ativo" = true;

                        DELETE FROM "CategoriaSubCategoria"
                        WHERE "SubCategoriaId" = NEW."Id";
                    END IF;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                DROP TRIGGER IF EXISTS trg_cascade_subcategoria_inativada ON "SubCategorias";
                CREATE TRIGGER trg_cascade_subcategoria_inativada
                AFTER UPDATE OF "Ativo" ON "SubCategorias"
                FOR EACH ROW
                EXECUTE FUNCTION fn_cascade_subcategoria_inativada();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS trg_cascade_subcategoria_inativada ON "SubCategorias";
                DROP FUNCTION IF EXISTS fn_cascade_subcategoria_inativada();
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Transacoes_Categorias_CategoriaId",
                table: "Transacoes");

            migrationBuilder.DropTable(
                name: "CategoriaSubCategoria");

            migrationBuilder.DropIndex(
                name: "IX_Transacoes_CategoriaId",
                table: "Transacoes");

            migrationBuilder.AddColumn<int>(
                name: "CategoriaId",
                table: "SubCategorias",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "Transacoes");

            migrationBuilder.DropColumn(
                name: "CategoriaId",
                table: "Transacoes");

            migrationBuilder.AlterColumn<string>(
                name: "Telefone",
                table: "Membros",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Membros",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "Necessario",
                table: "Categorias",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubCategorias_CategoriaId",
                table: "SubCategorias",
                column: "CategoriaId");

            migrationBuilder.AddForeignKey(
                name: "FK_SubCategorias_Categorias_CategoriaId",
                table: "SubCategorias",
                column: "CategoriaId",
                principalTable: "Categorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
