using System.ComponentModel.DataAnnotations;

namespace GerenciadorContaFamiliar.Application.DTOs.Requests.Notificacoes;

public class CriarNotificacaoRequest
{
    [Required(ErrorMessage = "A mensagem é obrigatória")]
    [StringLength(500, MinimumLength = 2, ErrorMessage = "A mensagem deve ter entre 2 e 500 caracteres")]
    public string Mensagem { get; set; } = null!;

    /// <summary>Injetado automaticamente pelo controller a partir do JWT.</summary>
    public int MembroId { get; set; }
}
