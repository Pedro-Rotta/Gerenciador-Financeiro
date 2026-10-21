using GerenciadorContaFamiliar.Application.DTOs.Requests.Relatorios;
using GerenciadorContaFamiliar.Application.DTOs.Responses;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Relatorios;

public interface IRelatorioGastosUseCase
{
    Task<RelatorioGastosResponse> ExecutarAsync(
        RelatorioGastosRequest request,
        CancellationToken cancellationToken = default);
}
