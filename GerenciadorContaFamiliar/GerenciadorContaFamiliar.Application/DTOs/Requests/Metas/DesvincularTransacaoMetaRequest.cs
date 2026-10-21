using System.ComponentModel.DataAnnotations;

namespace GerenciadorContaFamiliar.Application.DTOs.Requests.Metas;

public class DesvincularTransacaoMetaRequest
{
    [Required(ErrorMessage = "O ID da meta é obrigatório")]
    [Range(1, int.MaxValue, ErrorMessage = "ID da meta inválido")]
    public int MetaId { get; set; }

    [Required(ErrorMessage = "O ID da transação é obrigatório")]
    [Range(1, int.MaxValue, ErrorMessage = "ID da transação inválido")]
    public int TransacaoId { get; set; }

    /// <summary>Injetado automaticamente pelo controller a partir do JWT.</summary>
    public int MembroId { get; set; }
}
