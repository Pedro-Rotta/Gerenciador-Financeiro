using GerenciadorContaFamiliar.Application.DTOs.Requests.Verificacao;
using GerenciadorContaFamiliar.Application.DTOs.Responses;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Verificacao;

public interface IExecutarVerificacaoUseCase
{
    Task<VerificacaoResponse> ExecutarAsync(
        ExecutarVerificacaoRequest request,
        CancellationToken cancellationToken = default);
}
