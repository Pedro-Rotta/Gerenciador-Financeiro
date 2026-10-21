using System.ComponentModel.DataAnnotations;

namespace GerenciadorContaFamiliar.Application.DTOs.Requests.SubCategorias;

public class CriarSubCategoriaRequest
{
    [Required(ErrorMessage = "O nome é obrigatório")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 100 caracteres")]
    public string Nome { get; set; } = null!;

    /// <summary>
    /// ID da Conta individual à qual esta subcategoria pertence.
    /// Exatamente um de ContaId ou ContaConjuntaId deve ser informado.
    /// </summary>
    public int? ContaId { get; set; }

    /// <summary>
    /// ID da ContaConjunta à qual esta subcategoria pertence.
    /// Exatamente um de ContaId ou ContaConjuntaId deve ser informado.
    /// </summary>
    public int? ContaConjuntaId { get; set; }

    /// <summary>Injetado automaticamente pelo controller a partir do JWT.</summary>
    public int MembroId { get; set; }
}
