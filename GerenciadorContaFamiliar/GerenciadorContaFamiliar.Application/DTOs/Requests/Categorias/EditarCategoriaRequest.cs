using System.ComponentModel.DataAnnotations;
using GerenciadorContaFamiliar.Domain;
namespace GerenciadorContaFamiliar.Application.DTOs.Requests.Categorias;

public class EditarCategoriaRequest
{
    /// <summary>Injetado automaticamente pelo controller a partir da URL.</summary>
    public int Id { get; set; }

    [StringLength(100, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 100 caracteres")]
    public string? Nome { get; set; }

    [EnumDataType(typeof(Categoria.TipoCategoria), ErrorMessage = "Tipo inválido")]
    public Categoria.TipoCategoria? Tipo { get; set; }

    /// <summary>Opcional: quando não enviado, mantém o valor atual da categoria.</summary>
    public bool? Necessario { get; set; }

    [StringLength(100, MinimumLength = 2, ErrorMessage = "O Ícone deve ter entre 2 e 100 caracteres")]
    public string? Icone { get; set; }

    /// <summary>Injetado automaticamente pelo controller a partir do JWT.</summary>
    public int MembroId { get; set; }
}