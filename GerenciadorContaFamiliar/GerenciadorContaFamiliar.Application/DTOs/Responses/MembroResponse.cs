namespace GerenciadorContaFamiliar.Application.DTOs.Responses;

public class MembroResponse
{
    public int Id { get; set; }
    public string Nome { get; set; } = null!;
    public string NomeDeUsuario { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Telefone { get; set; }
    public DateOnly? DataNascimento { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
    public int? ContaPrincipalId { get; set; }
}
