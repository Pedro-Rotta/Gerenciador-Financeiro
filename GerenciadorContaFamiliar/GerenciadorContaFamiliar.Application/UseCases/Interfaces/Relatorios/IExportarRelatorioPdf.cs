using GerenciadorContaFamiliar.Application.DTOs.Requests.Relatorios;
using GerenciadorContaFamiliar.Application.DTOs.Responses;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Relatorios;

public interface IExportarRelatorioPdfUseCase
{
    Task<RelatorioPdfResponse> ExecutarAsync(
        RelatorioResumoRequest request,
        CancellationToken cancellationToken = default);
}
