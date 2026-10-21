using GerenciadorContaFamiliar.Domain;

namespace GerenciadorContaFamiliar.Application.DTOs.Responses;

/// <summary>Conteúdo do PDF de transações (já filtrado e ordenado) entregue ao gerador.</summary>
public class TransacoesPdfDados
{
    public string NomeMembro { get; set; } = null!;

    /// <summary>Filtros aplicados, já descritos (ex.: "Período" → "Esta semana (05/10/2026 a 07/10/2026)").</summary>
    public IReadOnlyList<(string Rotulo, string Valor)> Filtros { get; set; } = new List<(string, string)>();

    /// <summary>Transações da mais recente para a mais antiga.</summary>
    public IReadOnlyList<TransacaoPdfItem> Itens { get; set; } = new List<TransacaoPdfItem>();
}

public class TransacaoPdfItem
{
    public DateOnly DataTransacao { get; set; }
    public string Descricao { get; set; } = null!;
    public string Categoria { get; set; } = null!;
    public string Conta { get; set; } = null!;
    public Transacao.TipoTransacao Tipo { get; set; }
    public Transacao.StatusTransacao Status { get; set; }
    public decimal Valor { get; set; }
}
