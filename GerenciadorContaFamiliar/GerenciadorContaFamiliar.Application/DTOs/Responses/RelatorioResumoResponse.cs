namespace GerenciadorContaFamiliar.Application.DTOs.Responses;

/// <summary>
/// Resumo financeiro de um período (1, 3, 6 ou 12 meses terminando em Ano/Mes):
/// cards do resumo executivo, despesas do período e comparativo mensal. Considera
/// as contas individuais do membro e as contas conjuntas das quais ele participa.
/// Os campos "...Mes" mantêm o nome por compatibilidade, mas valem para o período.
/// </summary>
public class RelatorioResumoResponse
{
    /// <summary>Último mês do período.</summary>
    public int Ano { get; set; }
    public int Mes { get; set; }

    /// <summary>Quantidade de meses do período (1, 3, 6 ou 12).</summary>
    public int Meses { get; set; }

    /// <summary>Primeiro e último dia do período.</summary>
    public DateOnly DataInicio { get; set; }
    public DateOnly DataFim { get; set; }

    /// <summary>Total de saídas do período dividido pelos dias considerados (até hoje, se o período inclui o mês atual).</summary>
    public decimal MediaGastosDiarios { get; set; }

    /// <summary>Média diária de gastos do período anterior, de mesmo tamanho (para comparação).</summary>
    public decimal MediaGastosDiariosMesAnterior { get; set; }

    /// <summary>Receitas e despesas do período.</summary>
    public decimal ReceitasMes { get; set; }
    public decimal DespesasMes { get; set; }

    /// <summary>Percentual da receita que sobrou no período. Null quando não houve receita.</summary>
    public decimal? EconomiaPercentual { get; set; }

    /// <summary>Categoria com o maior total de saídas no período. Null quando não houve saídas.</summary>
    public string? MaiorCategoriaNome { get; set; }
    public decimal MaiorCategoriaValor { get; set; }

    /// <summary>
    /// Quanto guardar por mês para concluir as metas em andamento no prazo: soma, de cada
    /// meta, do valor que falta ÷ meses até o prazo (contando o mês atual). Usa a situação
    /// atual das metas, qualquer que seja o mês do relatório.
    /// </summary>
    public decimal AporteMensalMetas { get; set; }

    /// <summary>Quantidade de metas em andamento (não concluídas).</summary>
    public int MetasEmAndamento { get; set; }

    /// <summary>Todas as saídas do período, da maior para a menor (a tela exibe as 5 primeiras).</summary>
    public IReadOnlyList<RelatorioDespesaItem> Despesas { get; set; } = new List<RelatorioDespesaItem>();

    /// <summary>
    /// Receita, despesa e resultado de cada mês, do mês da primeira transação até o mês
    /// do relatório (no mínimo 6 meses, ou o tamanho do período se maior; o último é o mês do relatório).
    /// </summary>
    public IReadOnlyList<RelatorioMesItem> Comparativo { get; set; } = new List<RelatorioMesItem>();
}

public class RelatorioDespesaItem
{
    /// <summary>Descrição da transação ("Sem descrição" quando vazia).</summary>
    public string Descricao { get; set; } = null!;

    /// <summary>Nome da categoria ("Sem categoria" quando não há).</summary>
    public string Categoria { get; set; } = null!;

    public decimal Valor { get; set; }
    public DateOnly DataTransacao { get; set; }
}

public class RelatorioMesItem
{
    public int Ano { get; set; }
    public int Mes { get; set; }
    public decimal Receita { get; set; }
    public decimal Despesa { get; set; }
    public decimal Resultado { get; set; }

    /// <summary>Variação % do resultado em relação ao mês anterior. Null no 1º mês ou quando o anterior foi zero.</summary>
    public decimal? VariacaoPercentual { get; set; }
}
