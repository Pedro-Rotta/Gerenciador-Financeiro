using System.ComponentModel.DataAnnotations;

namespace GerenciadorContaFamiliar.Application.DTOs.Requests.Auth;

/// <summary>POST api/Auth/redefinir-senha — troca a senha com o código recebido.</summary>
public class RedefinirSenhaRequest
{
    [Required(ErrorMessage = "Informe o nome de usuário ou o e-mail")]
    [StringLength(100, ErrorMessage = "O nome de usuário ou e-mail deve ter no máximo 100 caracteres")]
    public string Identificador { get; set; } = null!;

    [Required(ErrorMessage = "Informe o código recebido por e-mail")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "O código tem 6 números")]
    public string Codigo { get; set; } = null!;

    [Required(ErrorMessage = "A nova senha é obrigatória")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter entre 6 e 100 caracteres")]
    public string NovaSenha { get; set; } = null!;

    [Required(ErrorMessage = "A confirmação da senha é obrigatória")]
    [Compare(nameof(NovaSenha), ErrorMessage = "As senhas não coincidem")]
    public string ConfirmacaoSenha { get; set; } = null!;
}
