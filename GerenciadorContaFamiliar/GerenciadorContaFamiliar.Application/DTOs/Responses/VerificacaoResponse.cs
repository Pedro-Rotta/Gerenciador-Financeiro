namespace GerenciadorContaFamiliar.Application.DTOs.Responses;

public class VerificacaoResponse
{
    /// <summary>false = já rodou na última hora para este membro (nada foi feito).</summary>
    public bool Executada { get; set; }

    /// <summary>Passos que falharam, no formato "Passo: mensagem" (para o log).</summary>
    public List<string> Falhas { get; set; } = new();
}
