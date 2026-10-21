using System.ComponentModel.DataAnnotations;

namespace GerenciadorContaFamiliar.Application.DTOs.Requests.Transacoes;

public class ExcluirTransacaoRequest
{
    /// <summary>Em ocorrência de série: só esta (padrão) ou esta e as seguintes não concluídas.</summary>
    public enum EscopoExclusao { ESTA, PROXIMAS }

    [Required(ErrorMessage = "O ID é obrigatório")]
    [Range(1, int.MaxValue, ErrorMessage = "ID inválido")]
    public int Id { get; set; }

    /// <summary>Injetado automaticamente pelo controller a partir do JWT.</summary>
    public int MembroId { get; set; }

    public EscopoExclusao Escopo { get; set; } = EscopoExclusao.ESTA;
}
