namespace GerenciadorContaFamiliar.Application.DTOs.Responses;

/// <summary>PDF do relatório financeiro, pronto para download.</summary>
public class RelatorioPdfResponse
{
    public byte[] Conteudo { get; set; } = Array.Empty<byte>();

    /// <summary>Ex.: "relatorio-2026-10.pdf" ou "relatorio-2026-08-a-2026-10.pdf".</summary>
    public string NomeArquivo { get; set; } = null!;
}
