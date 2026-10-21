using GerenciadorContaFamiliar.Domain;

namespace GerenciadorContaFamiliar.Application.DTOs.Responses;

/// <summary>Ocorrência futura de uma série mensal, ainda não lançada (não entra no saldo).</summary>
public class OcorrenciaPrevistaResponse
{
    public int RecorrenciaId { get; set; }
    public DateOnly DataPrevista { get; set; }
    /// <summary>Posição na série (ex.: 6 em "6 de 10").</summary>
    public int NumeroOcorrencia { get; set; }
    /// <summary>Quantidade de vezes da série (null = sem fim).</summary>
    public int? TotalOcorrencias { get; set; }
    public decimal Valor { get; set; }
    public string? Descricao { get; set; }
    public Transacao.TipoTransacao Tipo { get; set; }
    public int MembroId { get; set; }
    public int? CategoriaId { get; set; }
    public int? ContaId { get; set; }
    public int? ContaConjuntaId { get; set; }
}
