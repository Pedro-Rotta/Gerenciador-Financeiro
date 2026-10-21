using GerenciadorContaFamiliar.Domain;

namespace GerenciadorContaFamiliar.Application.DTOs.Responses;

public class TransacaoResponse
{
    public int Id { get; set; }
    public decimal Valor { get; set; }
    public string? Descricao { get; set; }
    public DateOnly DataTransacao { get; set; }
    public Transacao.TipoTransacao Tipo { get; set; }
    public Transacao.ClassificacaoTransacao Classificacao { get; set; }
    public bool Recorrente { get; set; }
    public Transacao.PeriodicidadeTransacao? Periodicidade { get; set; }
    public Transacao.StatusTransacao Status { get; set; }
    public int MembroId { get; set; }
    public int? CategoriaId { get; set; }
    public int? SubCategoriaId { get; set; }
    public int? ContaId { get; set; }
    public int? ContaConjuntaId { get; set; }
    /// <summary>Série que gerou a transação (null = lançamento avulso).</summary>
    public int? RecorrenciaId { get; set; }
    /// <summary>Posição na série (ex.: 3 em "3 de 10").</summary>
    public int? NumeroOcorrencia { get; set; }
    /// <summary>Quantidade de vezes da série (null = sem fim).</summary>
    public int? TotalOcorrencias { get; set; }
    /// <summary>A série ainda repete (false = encerrada).</summary>
    public bool? SerieAtiva { get; set; }
    public DateTime CriadoEm { get; set; }
    public bool Ativo { get; set; }
}
