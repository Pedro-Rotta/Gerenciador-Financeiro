using GerenciadorContaFamiliar.Application.DTOs.Responses;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Relatorios;

/// <summary>
/// Abstração da geração do PDF do relatório financeiro.
/// A implementação concreta (QuestPDF) fica no projeto web, junto da apresentação.
/// </summary>
public interface IGeradorPdfRelatorio
{
    /// <summary>Monta o PDF (A4) a partir do resumo do período.</summary>
    byte[] Gerar(RelatorioResumoResponse resumo, string nomeMembro);
}
