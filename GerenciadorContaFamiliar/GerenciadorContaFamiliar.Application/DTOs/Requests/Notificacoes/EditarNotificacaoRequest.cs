using System.ComponentModel.DataAnnotations;

namespace GerenciadorContaFamiliar.Application.DTOs.Requests.Notificacoes;

public class EditarNotificacaoRequest
{
    /// <summary>Injetado automaticamente pelo controller a partir da URL.</summary>
    public int Id { get; set; }

    [StringLength(500, MinimumLength = 2, ErrorMessage = "A mensagem deve ter entre 2 e 500 caracteres")]
    public string? Mensagem { get; set; }

    public bool? Lida { get; set; }

    /// <summary>Injetado automaticamente pelo controller a partir do JWT.</summary>
    public int MembroId { get; set; }
}
