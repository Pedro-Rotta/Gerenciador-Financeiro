namespace GerenciadorContaFamiliar.Application.DTOs.Responses;

public class RelatorioGastosResponse
{
    /// <summary>Total geral de todas as transações consideradas.</summary>
    public decimal ValorTotal { get; set; }

    /// <summary>Quantidade total de transações consideradas.</summary>
    public int QuantidadeTotal { get; set; }

    /// <summary>Itens agrupados (por categoria ou por subcategoria, conforme o filtro).</summary>
    public IReadOnlyList<RelatorioGastosItem> Itens { get; set; } = new List<RelatorioGastosItem>();
}

public class RelatorioGastosItem
{
    /// <summary>Nome do agrupador (nome da categoria OU da subcategoria; "Sem subcategoria" quando não há).</summary>
    public string Nome { get; set; } = null!;

    public decimal Valor { get; set; }

    public int Quantidade { get; set; }
}
