using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GerenciadorContaFamiliar.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExcluirSubCategoriaMantemTransacoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ─────────────────────────────────────────────────────────────
            // Inativar uma SubCategoria NÃO inativa mais as Transacoes dela.
            // Antes, as transações sumiam das listagens mas o saldo da conta
            // continuava contando o valor delas. Agora elas permanecem ativas
            // (aparecem como "sem subcategoria", pois a subcategoria inativa é
            // filtrada) e só os vínculos N:N com categorias são removidos.
            // ─────────────────────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION fn_cascade_subcategoria_inativada()
                RETURNS TRIGGER AS $$
                BEGIN
                    IF (OLD."Ativo" = true AND NEW."Ativo" = false) THEN
                        DELETE FROM "CategoriaSubCategoria"
                        WHERE "SubCategoriaId" = NEW."Id";
                    END IF;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restaura o comportamento anterior (inativa as transações da subcategoria).
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
                """);
        }
    }
}
