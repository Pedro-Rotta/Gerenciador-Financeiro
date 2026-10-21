using GerenciadorContaFamiliar.Application.DTOs.Requests.Transacoes;
using GerenciadorContaFamiliar.Application.DTOs.Responses;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Transacoes;

public interface IExportarTransacoesPdfUseCase
{
    Task<RelatorioPdfResponse> ExecutarAsync(
        ExportarTransacoesPdfRequest request,
        CancellationToken cancellationToken = default);
}
