using GerenciadorContaFamiliar.Domain;
namespace GerenciadorContaFamiliar.Application.DTOs.Responses;

public class CategoriaResponse
{
    public int Id { get; set; }
    public string Nome { get; set; } = null!;
    public Categoria.TipoCategoria Tipo { get; set; }
    public bool Necessario { get; set; }
    public string? Icone { get; set; }
    public bool Ativo { get; set; }
    public int? ContaId { get; set; }
    public int? ContaConjuntaId { get; set; }
    public IReadOnlyList<SubCategoriaResponse> SubCategorias { get; set; } = new List<SubCategoriaResponse>();
}
