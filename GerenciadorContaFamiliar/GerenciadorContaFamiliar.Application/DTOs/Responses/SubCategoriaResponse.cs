namespace GerenciadorContaFamiliar.Application.DTOs.Responses;

public class SubCategoriaResponse
{
    public int Id { get; set; }
    public string Nome { get; set; } = null!;
    public bool Ativo { get; set; }
    public int? ContaId { get; set; }
    public int? ContaConjuntaId { get; set; }
}
