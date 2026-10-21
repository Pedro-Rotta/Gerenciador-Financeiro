using System.ComponentModel.DataAnnotations;

namespace GerenciadorContaFamiliar.Application.DTOs.Requests.Contas;

public class CriarContaRequest
{
    [Required(ErrorMessage = "O nome é obrigatório")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 100 caracteres")]
    public string Nome { get; set; } = null!;

    /// <summary>Injetado automaticamente pelo controller a partir do JWT.</summary>
    public int MembroId { get; set; }

    /// <summary>
    /// Indica se esta conta deve ser a conta principal do membro.
    /// Cada membro possui exatamente uma conta principal.
    /// </summary>
    public bool ContaPrincipal { get; set; }
}