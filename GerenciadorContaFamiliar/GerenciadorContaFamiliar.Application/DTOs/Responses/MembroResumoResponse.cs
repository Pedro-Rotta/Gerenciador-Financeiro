namespace GerenciadorContaFamiliar.Application.DTOs.Responses;

/// <summary>Dados públicos de um membro (ex.: participantes de uma conta conjunta).</summary>
public class MembroResumoResponse
{
    public int Id { get; set; }
    public string Nome { get; set; } = null!;
    public string NomeDeUsuario { get; set; } = null!;
}
