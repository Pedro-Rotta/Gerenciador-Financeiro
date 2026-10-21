using System.ComponentModel.DataAnnotations;

namespace GerenciadorContaFamiliar.Application.DTOs.Requests.ContasConjuntas;

public class RemoverParticipanteRequest
{
    /// <summary>Injetado automaticamente pelo controller a partir da URL.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "ID inválido")]
    public int Id { get; set; }

    /// <summary>Membro a ser removido. Injetado automaticamente pelo controller a partir da URL.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Participante inválido")]
    public int ParticipanteId { get; set; }

    /// <summary>Injetado automaticamente pelo controller a partir do JWT.</summary>
    public int MembroId { get; set; }
}
