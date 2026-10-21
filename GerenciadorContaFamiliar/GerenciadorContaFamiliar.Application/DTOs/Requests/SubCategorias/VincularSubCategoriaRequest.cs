using System.ComponentModel.DataAnnotations;

namespace GerenciadorContaFamiliar.Application.DTOs.Requests.SubCategorias;

public class VincularSubCategoriaRequest
{
    public int MembroId { get; set; }

    [Required(ErrorMessage = "O ID da categoria é obrigatório")]
    [Range(1, int.MaxValue, ErrorMessage = "ID da categoria inválido")]
    public int IdCategoria { get; set; }

    [Required(ErrorMessage = "O ID da subcategoria é obrigatório")]
    [Range(1, int.MaxValue, ErrorMessage = "ID da subcategoria inválido")]
    public int IdSubCategoria { get; set; }
}
