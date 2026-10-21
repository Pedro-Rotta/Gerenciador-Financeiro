using System.ComponentModel.DataAnnotations;

namespace GerenciadorContaFamiliar.Application.DTOs.Requests.Convites;

public class ListarConvitesDaContaRequest
{
    [Required(ErrorMessage = "A conta conjunta é obrigatória")]
    [Range(1, int.MaxValue, ErrorMessage = "ID da conta conjunta inválido")]
    public int ContaConjuntaId { get; set; }

    /// <summary>Injetado automaticamente pelo controller a partir do JWT.</summary>
    public int MembroId { get; set; }
}
