using GerenciadorContaFamiliar.Application.DTOs.Requests.Relatorios;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Relatorios;

namespace GerenciadorContaFamiliar.Application.UseCases.Relatorios;

/// <summary>
/// Resumo financeiro de um período — mês, trimestre, semestre ou ano terminando
/// no mês escolhido (página Relatórios):
/// - Média de gastos diários (e a do período anterior de mesmo tamanho, para comparação);
/// - Economia (% da receita que sobrou no período);
/// - Maior categoria de gastos;
/// - Quanto guardar por mês para concluir as metas em andamento no prazo;
/// - Despesas do período, da maior para a menor (a tela exibe o top 5 e expande);
/// - Comparativo mensal desde a primeira transação, no mínimo 6 meses (ou o
///   tamanho do período, se maior) — receita, despesa, resultado e variação %.
/// Considera as contas individuais do membro e as conjuntas das quais ele participa.
/// </summary>
public class RelatorioResumoUseCase : IRelatorioResumoUseCase
{
    private const int MesesMinimosNoComparativo = 6;
    private static readonly int[] MesesPermitidos = { 1, 3, 6, 12 };

    private readonly ITransacaoRepository _transacaoRepository;
    private readonly IMetaRepository _metaRepository;

    public RelatorioResumoUseCase(
        ITransacaoRepository transacaoRepository,
        IMetaRepository metaRepository)
    {
        _transacaoRepository = transacaoRepository ?? throw new ArgumentNullException(nameof(transacaoRepository));
        _metaRepository = metaRepository ?? throw new ArgumentNullException(nameof(metaRepository));
    }

    public async Task<RelatorioResumoResponse> ExecutarAsync(
        RelatorioResumoRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (request.Ano.HasValue != request.Mes.HasValue)
            throw new ArgumentException("Informe o ano e o mês juntos.");

        // 1. Mês do relatório (padrão: mês atual). Meses futuros não têm dados.
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        var inicioMesAtual = new DateOnly(hoje.Year, hoje.Month, 1);
        var inicioMes = request.Ano.HasValue
            ? new DateOnly(request.Ano.Value, request.Mes!.Value, 1)
            : inicioMesAtual;

        if (inicioMes > inicioMesAtual)
            throw new ArgumentException("Não é possível gerar o relatório de um mês futuro.");

        // Período: "meses" meses terminando no mês do relatório (padrão: só o mês)
        int meses = request.Meses ?? 1;
        if (!MesesPermitidos.Contains(meses))
            throw new ArgumentException("Período inválido: use 1, 3, 6 ou 12 meses.");

        var fimMes = inicioMes.AddMonths(1).AddDays(-1);
        var inicioPeriodo = inicioMes.AddMonths(-(meses - 1));
        // No período que inclui o mês atual, a média diária conta só até hoje
        var ultimoDiaConsiderado = fimMes > hoje ? hoje : fimMes;
        int diasConsiderados = ultimoDiaConsiderado.DayNumber - inicioPeriodo.DayNumber + 1;

        // 2. Transações das contas do membro até o fim do mês (o comparativo vai até a primeira).
        // Só as pagas/recebidas, como o saldo: pendentes e atrasadas entram quando forem pagas
        var transacoes = (await _transacaoRepository.ObterParaRelatorioAsync(
            request.MembroId,
            idCategoria: null,
            idSubCategoria: null,
            apenasSemSubcategoria: false,
            dataInicio: null,
            dataFim: fimMes,
            ct: cancellationToken))
            .Where(t => t.Status == Transacao.StatusTransacao.CONCLUIDA)
            .ToList();

        var doPeriodo = DoPeriodo(transacoes, inicioPeriodo, fimMes);
        var despesasDoPeriodo = doPeriodo.Where(t => !t.EhEntrada()).ToList();
        decimal receitas = doPeriodo.Where(t => t.EhEntrada()).Sum(t => t.Valor);
        decimal despesas = despesasDoPeriodo.Sum(t => t.Valor);

        // 3. Média de gastos diários e a do período anterior, de mesmo tamanho
        var inicioPeriodoAnterior = inicioPeriodo.AddMonths(-meses);
        var fimPeriodoAnterior = inicioPeriodo.AddDays(-1);
        decimal despesasPeriodoAnterior = DoPeriodo(transacoes, inicioPeriodoAnterior, fimPeriodoAnterior)
            .Where(t => !t.EhEntrada())
            .Sum(t => t.Valor);
        int diasPeriodoAnterior = fimPeriodoAnterior.DayNumber - inicioPeriodoAnterior.DayNumber + 1;

        // 4. Economia: % da receita que sobrou no período
        decimal? economia = receitas > 0
            ? Math.Round((receitas - despesas) / receitas * 100, 1)
            : null;

        // 5. Maior categoria de gastos do período
        var maiorCategoria = despesasDoPeriodo
            .GroupBy(t => t.Categoria?.Nome ?? "Sem categoria")
            .Select(g => new { Nome = g.Key, Valor = g.Sum(t => t.Valor) })
            .OrderByDescending(g => g.Valor)
            .FirstOrDefault();

        // 6. Quanto guardar por mês para concluir as metas em andamento no prazo.
        //    Situação atual das metas: o que falta ÷ meses até o prazo (contando o mês atual);
        //    meta que vence neste mês ou já venceu precisa do valor que falta de uma vez.
        var metas = await _metaRepository.ObterPorMembroAsync(request.MembroId, cancellationToken);
        var metasEmAndamento = metas
            .Where(m => !m.Concluida && m.ValorAtual < m.ValorAlvo)
            .ToList();
        decimal aporteMensalMetas = metasEmAndamento.Sum(m =>
        {
            int mesesAtePrazo = (m.Prazo.Year - hoje.Year) * 12 + m.Prazo.Month - hoje.Month + 1;
            return (m.ValorAlvo - m.ValorAtual) / Math.Max(mesesAtePrazo, 1);
        });

        // 7. Despesas do período, da maior para a menor
        var despesasOrdenadas = despesasDoPeriodo
            .OrderByDescending(t => t.Valor)
            .ThenByDescending(t => t.DataTransacao)
            .Select(t => new RelatorioDespesaItem
            {
                Descricao = string.IsNullOrWhiteSpace(t.Descricao) ? "Sem descrição" : t.Descricao,
                Categoria = t.Categoria?.Nome ?? "Sem categoria",
                Valor = t.Valor,
                DataTransacao = t.DataTransacao,
            })
            .ToList();

        // 8. Comparativo mensal: do mês da primeira transação até o mês do relatório
        //    (no mínimo 6 meses, ou o período inteiro se for maior), com a variação
        //    do resultado em relação ao mês anterior
        var inicioComparativo = inicioMes.AddMonths(-(Math.Max(MesesMinimosNoComparativo, meses) - 1));
        if (transacoes.Count > 0)
        {
            var primeiraData = transacoes.Min(t => t.DataTransacao);
            var inicioPrimeiroMes = new DateOnly(primeiraData.Year, primeiraData.Month, 1);
            if (inicioPrimeiroMes < inicioComparativo)
                inicioComparativo = inicioPrimeiroMes;
        }

        var comparativo = new List<RelatorioMesItem>();
        decimal? resultadoAnterior = null;
        for (var inicio = inicioComparativo; inicio <= inicioMes; inicio = inicio.AddMonths(1))
        {
            var transacoesDoMes = DoMes(transacoes, inicio);
            decimal receita = transacoesDoMes.Where(t => t.EhEntrada()).Sum(t => t.Valor);
            decimal despesa = transacoesDoMes.Where(t => !t.EhEntrada()).Sum(t => t.Valor);
            decimal resultado = receita - despesa;

            decimal? variacao = resultadoAnterior is decimal anterior && anterior != 0
                ? Math.Round((resultado - anterior) / Math.Abs(anterior) * 100, 1)
                : null;

            comparativo.Add(new RelatorioMesItem
            {
                Ano = inicio.Year,
                Mes = inicio.Month,
                Receita = receita,
                Despesa = despesa,
                Resultado = resultado,
                VariacaoPercentual = variacao,
            });
            resultadoAnterior = resultado;
        }

        // 9. Montar resposta
        return new RelatorioResumoResponse
        {
            Ano = inicioMes.Year,
            Mes = inicioMes.Month,
            Meses = meses,
            DataInicio = inicioPeriodo,
            DataFim = fimMes,
            MediaGastosDiarios = Math.Round(despesas / diasConsiderados, 2),
            MediaGastosDiariosMesAnterior = Math.Round(despesasPeriodoAnterior / diasPeriodoAnterior, 2),
            ReceitasMes = receitas,
            DespesasMes = despesas,
            EconomiaPercentual = economia,
            MaiorCategoriaNome = maiorCategoria?.Nome,
            MaiorCategoriaValor = maiorCategoria?.Valor ?? 0,
            AporteMensalMetas = Math.Round(aporteMensalMetas, 2),
            MetasEmAndamento = metasEmAndamento.Count,
            Despesas = despesasOrdenadas,
            Comparativo = comparativo,
        };
    }

    // Transações cuja data está dentro do mês que começa em "inicioMes"
    private static List<Transacao> DoMes(IEnumerable<Transacao> transacoes, DateOnly inicioMes)
        => DoPeriodo(transacoes, inicioMes, inicioMes.AddMonths(1).AddDays(-1));

    // Transações com data entre "inicio" e "fim" (inclusive)
    private static List<Transacao> DoPeriodo(IEnumerable<Transacao> transacoes, DateOnly inicio, DateOnly fim)
    {
        return transacoes
            .Where(t => t.DataTransacao >= inicio && t.DataTransacao <= fim)
            .ToList();
    }
}
