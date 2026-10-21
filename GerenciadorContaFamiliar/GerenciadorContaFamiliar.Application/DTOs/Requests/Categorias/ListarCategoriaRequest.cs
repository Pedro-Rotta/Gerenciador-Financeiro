using System.ComponentModel.DataAnnotations;

namespace GerenciadorContaFamiliar.Application.DTOs.Requests.Categorias;

public class ListarCategoriaRequest
{
    public int? ContaId { get; set; }
    public int? ContaConjuntaId { get; set;}
    
    public int MembroId { get; set; } 
}