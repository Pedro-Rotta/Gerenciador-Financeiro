using GerenciadorContaFamiliar.Domain;

namespace GerenciadorContaFamiliar.Application.DTOs.Responses;

public class ConviteResponse
{
    public int Id { get; set; }
    public int ContaConjuntaId { get; set; }
    public string ContaConjuntaNome { get; set; } = null!;
    public int ConvidadoId { get; set; }
    public string ConvidadoNome { get; set; } = null!;
    public string ConvidadoNomeDeUsuario { get; set; } = null!;
    public int ConvidadoPorId { get; set; }
    public string ConvidadoPorNome { get; set; } = null!;
    public ConviteContaConjunta.StatusConvite Status { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? RespondidoEm { get; set; }
}
