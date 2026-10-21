using System.ComponentModel.DataAnnotations;

namespace GerenciadorContaFamiliar.Application.DTOs.Requests.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "O nome de usuário é obrigatório.")]
    public string NomeDeUsuario { get; set; } = null!;

    [Required(ErrorMessage = "A senha é obrigatória.")]
    public string Senha { get; set; } = null!;
}
