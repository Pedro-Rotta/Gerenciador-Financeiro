using GerenciadorContaFamiliar.Application.DTOs.Requests.Recorrencias;
using GerenciadorContaFamiliar.Application.DTOs.Responses;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Recorrencias;

public interface IListarOcorrenciasPrevistasUseCase
{
    Task<IEnumerable<OcorrenciaPrevistaResponse>> ExecutarAsync(
        ListarOcorrenciasPrevistasRequest request,
        CancellationToken cancellationToken = default);
}
