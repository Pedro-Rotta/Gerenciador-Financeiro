using GerenciadorContaFamiliar.Application.DTOs.Requests.Recorrencias;
using GerenciadorContaFamiliar.Application.DTOs.Responses;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Recorrencias;

public interface IPararRecorrenciaUseCase
{
    Task<TransacaoResponse> ExecutarAsync(
        PararRecorrenciaRequest request,
        CancellationToken cancellationToken = default);
}
