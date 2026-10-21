using System.ComponentModel.DataAnnotations;

namespace GerenciadorContaFamiliar.Application.DTOs.Requests.Membros;

public class EditarMembroRequest
{
    /// <summary>Injetado automaticamente pelo controller a partir do JWT (o corpo não precisa mandar).</summary>
    public int Id { get; set; }

    [StringLength(100, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 100 caracteres")]
    public string? Nome { get; set; }

    [StringLength(50, MinimumLength = 3, ErrorMessage = "O nome de usuário deve ter entre 3 e 50 caracteres")]
    public string? NomeDeUsuario { get; set; }

    [EmailAddress(ErrorMessage = "E-mail inválido")]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "Telefone inválido")]
    public string? Telefone { get; set; }

    [DataType(DataType.Date)]
    public DateOnly? DataNascimento { get; set; }

    /// <summary>Injetado automaticamente pelo controller a partir do JWT.</summary>
    public int MembroId { get; set; }
}
