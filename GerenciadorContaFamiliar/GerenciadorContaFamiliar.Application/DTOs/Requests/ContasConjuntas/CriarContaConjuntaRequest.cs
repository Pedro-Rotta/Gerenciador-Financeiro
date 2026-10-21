using System.ComponentModel.DataAnnotations;

namespace GerenciadorContaFamiliar.Application.DTOs.Requests.ContasConjuntas;

public class CriarContaConjuntaRequest
{
    [Required(ErrorMessage = "O nome é obrigatório")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 100 caracteres")]
    public string Nome { get; set; } = null!;

    [StringLength(100, MinimumLength = 2, ErrorMessage = "A descricao deve ter entre 2 e 100 caracteres")]
    public string? Descricao { get; set; }

    /// <summary>
    /// ID do Membro que está criando a conta. O criador é vinculado
    /// automaticamente como membro da conta conjunta.
    /// Valor injetado pelo controller a partir do token JWT (não vem do body).
    /// </summary>
    public int CriadorId { get; set; }
}