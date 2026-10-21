using System.ComponentModel.DataAnnotations;
using GerenciadorContaFamiliar.Domain;

namespace GerenciadorContaFamiliar.Application.DTOs.Requests.Categorias;

public class CriarCategoriaRequest
{
    [Required(ErrorMessage = "O nome é obrigatório")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 100 caracteres")]
    public string Nome { get; set; } = null!;

    [Required(ErrorMessage = "O tipo é obrigatório")]
    [EnumDataType(typeof(Categoria.TipoCategoria), ErrorMessage = "Tipo inválido")]
    public Categoria.TipoCategoria Tipo { get; set; }

    public bool Necessario { get; set; }

    [StringLength(100, MinimumLength = 2, ErrorMessage = "O Ícone deve ter entre 2 e 100 caracteres")]
    public string? Icone { get; set; }

    /// <summary>
    /// ID da Conta individual à qual esta categoria pertence.
    /// Exatamente um de ContaId ou ContaConjuntaId deve ser informado.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "ID inválido")]
    public int? ContaId { get; set; }

    /// <summary>
    /// ID da ContaConjunta à qual esta categoria pertence.
    /// Exatamente um de ContaId ou ContaConjuntaId deve ser informado.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "ID inválido")]
    public int? ContaConjuntaId { get; set; }

    /// <summary>Injetado automaticamente pelo controller a partir do JWT.</summary>
    public int MembroId { get; set; }
}