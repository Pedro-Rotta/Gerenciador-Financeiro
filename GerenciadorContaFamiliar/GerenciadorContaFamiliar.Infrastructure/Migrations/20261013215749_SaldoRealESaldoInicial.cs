using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GerenciadorContaFamiliar.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SaldoRealESaldoInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SaldoInicial",
                table: "Contas",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "SaldoInicialDefinido",
                table: "Contas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Contas que já tiveram alguma transação (mesmo excluída) ou têm série ativa ficam
            // liberadas com saldo inicial 0; as que nunca foram usadas vão pedir o valor
            migrationBuilder.Sql(@"
                UPDATE ""Contas"" c SET ""SaldoInicialDefinido"" =
                    EXISTS (SELECT 1 FROM ""Transacoes"" t WHERE t.""ContaId"" = c.""Id"")
                    OR EXISTS (SELECT 1 FROM ""Recorrencias"" r WHERE r.""ContaId"" = c.""Id"" AND r.""Ativa"");");

            // Saldo real: saldo inicial + entradas (Tipo 0) − saídas (Tipo 1) CONCLUÍDAS (Status 1) ativas
            migrationBuilder.Sql(@"
                UPDATE ""Contas"" c SET ""Saldo"" = c.""SaldoInicial"" + COALESCE((
                    SELECT SUM(CASE WHEN t.""Tipo"" = 0 THEN t.""Valor"" ELSE -t.""Valor"" END)
                    FROM ""Transacoes"" t
                    WHERE t.""ContaId"" = c.""Id"" AND t.""Ativo"" AND t.""Status"" = 1), 0);");
            migrationBuilder.Sql(@"
                UPDATE ""ContasConjuntas"" c SET ""Saldo"" = COALESCE((
                    SELECT SUM(CASE WHEN t.""Tipo"" = 0 THEN t.""Valor"" ELSE -t.""Valor"" END)
                    FROM ""Transacoes"" t
                    WHERE t.""ContaConjuntaId"" = c.""Id"" AND t.""Ativo"" AND t.""Status"" = 1), 0);");

            // Metas: soma real (sem teto) das vinculadas CONCLUÍDAS ativas; concluída enquanto alcançar o alvo
            migrationBuilder.Sql(@"
                UPDATE ""Metas"" m SET ""ValorAtual"" = COALESCE((
                    SELECT SUM(t.""Valor"")
                    FROM ""MetasTransacoes"" mt JOIN ""Transacoes"" t ON t.""Id"" = mt.""TransacaoId""
                    WHERE mt.""MetaId"" = m.""Id"" AND t.""Ativo"" AND t.""Status"" = 1), 0);");
            migrationBuilder.Sql(@"UPDATE ""Metas"" SET ""Concluida"" = ""ValorAtual"" >= ""ValorAlvo"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Volta à regra antiga: todo status ativo no saldo (sem saldo inicial), meta com teto no alvo
            migrationBuilder.Sql(@"
                UPDATE ""Contas"" c SET ""Saldo"" = COALESCE((
                    SELECT SUM(CASE WHEN t.""Tipo"" = 0 THEN t.""Valor"" ELSE -t.""Valor"" END)
                    FROM ""Transacoes"" t WHERE t.""ContaId"" = c.""Id"" AND t.""Ativo""), 0);");
            migrationBuilder.Sql(@"
                UPDATE ""ContasConjuntas"" c SET ""Saldo"" = COALESCE((
                    SELECT SUM(CASE WHEN t.""Tipo"" = 0 THEN t.""Valor"" ELSE -t.""Valor"" END)
                    FROM ""Transacoes"" t WHERE t.""ContaConjuntaId"" = c.""Id"" AND t.""Ativo""), 0);");
            migrationBuilder.Sql(@"
                UPDATE ""Metas"" m SET ""ValorAtual"" = LEAST(m.""ValorAlvo"", COALESCE((
                    SELECT SUM(t.""Valor"")
                    FROM ""MetasTransacoes"" mt JOIN ""Transacoes"" t ON t.""Id"" = mt.""TransacaoId""
                    WHERE mt.""MetaId"" = m.""Id"" AND t.""Ativo""), 0));");
            migrationBuilder.Sql(@"UPDATE ""Metas"" SET ""Concluida"" = ""ValorAtual"" >= ""ValorAlvo"";");

            migrationBuilder.DropColumn(
                name: "SaldoInicial",
                table: "Contas");

            migrationBuilder.DropColumn(
                name: "SaldoInicialDefinido",
                table: "Contas");
        }
    }
}
