using System.ComponentModel.DataAnnotations;

namespace GerenciadorContaFamiliar.Application.DTOs.Requests.Membros;

public class CriarMembroRequest
{
    [Required(ErrorMessage = "O nome é obrigatório")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 100 caracteres")]
    public string Nome { get; set; } = null!;

    [Required(ErrorMessage = "O nome de usuário é obrigatório")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "O nome de usuário deve ter entre 3 e 50 caracteres")]
    public string NomeDeUsuario { get; set; } = null!;

    [Required(ErrorMessage = "O email é obrigatório")]
    [EmailAddress(ErrorMessage = "E-mail inválido")]
    public string Email { get; set; } = null!;

    [Phone(ErrorMessage = "Telefone inválido")]
    public string? Telefone { get; set; } = null!;

    [Required(ErrorMessage = "A senha é obrigatória")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter entre 6 e 100 caracteres")]
    public string Senha { get; set; } = null!;

    /// <summary>Senha digitada de novo no cadastro: precisa ser idêntica a <see cref="Senha"/>.</summary>
    [Required(ErrorMessage = "A confirmação da senha é obrigatória")]
    [Compare(nameof(Senha), ErrorMessage = "As senhas não coincidem")]
    public string ConfirmacaoSenha { get; set; } = null!;

    [DataType(DataType.Date)]
    public DateOnly? DataNascimento { get; set; }
}
