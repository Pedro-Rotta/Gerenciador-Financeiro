using GerenciadorContaFamiliar.Application.DTOs.Requests.Contas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Contas;

public interface IDefinirSaldoInicialUseCase
{
    Task<ContaResponse> ExecutarAsync(
        DefinirSaldoInicialRequest request,
        CancellationToken cancellationToken = default);
}
