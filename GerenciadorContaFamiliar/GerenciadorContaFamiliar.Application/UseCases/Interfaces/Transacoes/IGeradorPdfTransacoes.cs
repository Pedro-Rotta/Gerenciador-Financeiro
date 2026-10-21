using GerenciadorContaFamiliar.Application.DTOs.Responses;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Transacoes;

/// <summary>
/// Abstração da geração do PDF de transações.
/// A implementação concreta (QuestPDF) fica no projeto web, junto da apresentação.
/// </summary>
public interface IGeradorPdfTransacoes
{
    byte[] Gerar(TransacoesPdfDados dados);
}
