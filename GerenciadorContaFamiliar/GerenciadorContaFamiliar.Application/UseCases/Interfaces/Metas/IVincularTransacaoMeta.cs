using GerenciadorContaFamiliar.Application.DTOs.Requests.Metas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Metas;

public interface IVincularTransacaoMetaUseCase
{
    Task<MetaResponse> ExecutarAsync(
        VincularTransacaoMetaRequest request,
        CancellationToken cancellationToken = default);
}
