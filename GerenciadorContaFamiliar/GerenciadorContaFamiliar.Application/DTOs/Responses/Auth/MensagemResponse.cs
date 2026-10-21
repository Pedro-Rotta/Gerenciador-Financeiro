namespace GerenciadorContaFamiliar.Application.DTOs.Responses.Auth;

/// <summary>Resposta só com uma mensagem para a tela (ex.: "Esqueceu a senha").</summary>
public class MensagemResponse
{
    public string Mensagem { get; set; } = null!;
}
