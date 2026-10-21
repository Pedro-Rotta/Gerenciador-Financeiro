using GerenciadorContaFamiliar.Application.DTOs.Requests.Relatorios;
using GerenciadorContaFamiliar.Application.DTOs.Responses;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Relatorios;

public interface IRelatorioResumoUseCase
{
    Task<RelatorioResumoResponse> ExecutarAsync(
        RelatorioResumoRequest request,
        CancellationToken cancellationToken = default);
}
