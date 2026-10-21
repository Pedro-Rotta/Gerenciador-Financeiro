using System.ComponentModel.DataAnnotations;

namespace GerenciadorContaFamiliar.Application.DTOs.Requests.Auth;

/// <summary>POST api/Auth/esqueci-senha — pede o código por e-mail.</summary>
public class EsqueciSenhaRequest
{
    /// <summary>Nome de usuário ou e-mail.</summary>
    [Required(ErrorMessage = "Informe o nome de usuário ou o e-mail")]
    [StringLength(100, ErrorMessage = "O nome de usuário ou e-mail deve ter no máximo 100 caracteres")]
    public string Identificador { get; set; } = null!;
}
