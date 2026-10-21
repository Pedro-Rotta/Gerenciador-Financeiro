using System.ComponentModel.DataAnnotations;

namespace GerenciadorContaFamiliar.Application.DTOs.Requests.Membros;

public class AlterarSenhaRequest
{
    [Required(ErrorMessage = "A senha atual é obrigatória")]
    public string SenhaAtual { get; set; } = null!;

    [Required(ErrorMessage = "A nova senha é obrigatória")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter entre 6 e 100 caracteres")]
    public string NovaSenha { get; set; } = null!;

    [Required(ErrorMessage = "A confirmação da senha é obrigatória")]
    [Compare(nameof(NovaSenha), ErrorMessage = "As senhas não coincidem")]
    public string ConfirmacaoSenha { get; set; } = null!;

    /// <summary>Injetado automaticamente pelo controller a partir do JWT.</summary>
    public int MembroId { get; set; }
}
