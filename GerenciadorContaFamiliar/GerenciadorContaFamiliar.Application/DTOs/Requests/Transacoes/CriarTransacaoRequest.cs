using System.ComponentModel.DataAnnotations;
using GerenciadorContaFamiliar.Domain;

namespace GerenciadorContaFamiliar.Application.DTOs.Requests.Transacoes;

public class CriarTransacaoRequest
{
    [Required(ErrorMessage = "O valor é obrigatório")]
    [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero")]
    public decimal Valor { get; set; }

    [StringLength(500, ErrorMessage = "A descrição deve ter no máximo 500 caracteres")]
    public string? Descricao { get; set; }

    [Required(ErrorMessage = "A data é obrigatório")]
    public DateOnly DataTransacao { get; set; }

    [Required(ErrorMessage = "O tipo é obrigatório")]
    public Transacao.TipoTransacao Tipo { get; set; }

    [Required(ErrorMessage = "A classificação é obrigatória")]
    public Transacao.ClassificacaoTransacao Classificacao { get; set; }

    public bool Recorrente { get; set; }

    public Transacao.PeriodicidadeTransacao? Periodicidade { get; set; }

    /// <summary>Só para recorrente: quantidade de vezes da série (vazio = sem fim).</summary>
    [Range(Recorrencia.MinimoOcorrencias, Recorrencia.MaximoOcorrencias,
        ErrorMessage = "A quantidade de vezes deve ficar entre 2 e 120")]
    public int? QuantidadeOcorrencias { get; set; }

    [Required(ErrorMessage = "O status é obrigatório")]
    public Transacao.StatusTransacao Status { get; set; } = Transacao.StatusTransacao.CONCLUIDA;
    public int MembroId { get; set; }
    public int? CategoriaId { get; set; }
    public int? SubCategoriaId { get; set; }
    public int? ContaId { get; set; }
    public int? ContaConjuntaId { get; set; }
}
