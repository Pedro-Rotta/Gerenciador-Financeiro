using System.ComponentModel.DataAnnotations;

namespace GerenciadorContaFamiliar.Application.DTOs.Requests.SubCategorias;

public class ListarSubCategoriaRequest
{
    public int MembroId { get; set; }
    public int? ContaId { get; set; }
    public int? ContaConjuntaId { get; set;}
}