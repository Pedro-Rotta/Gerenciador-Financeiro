using System.Text.Json.Serialization;

namespace GerenciadorContaFamiliar.Application.DTOs.Responses.Auth;

public class LoginResponse
{
    public int MembroId { get; set; }
    public string Nome { get; set; } = null!;
    public string NomeDeUsuario { get; set; } = null!;
    public string Email { get; set; } = null!;

    /// <summary>Vai só no cookie de sessão (HttpOnly), nunca no corpo da resposta.</summary>
    [JsonIgnore]
    public string Token { get; set; } = null!;

    public DateTime ExpiraEm { get; set; }
}
