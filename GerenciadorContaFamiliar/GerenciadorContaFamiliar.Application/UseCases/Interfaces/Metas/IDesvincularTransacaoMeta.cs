using GerenciadorContaFamiliar.Application.DTOs.Requests.Metas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Metas;

public interface IDesvincularTransacaoMetaUseCase
{
    Task<MetaResponse> ExecutarAsync(
        DesvincularTransacaoMetaRequest request,
        CancellationToken cancellationToken = default);
}
