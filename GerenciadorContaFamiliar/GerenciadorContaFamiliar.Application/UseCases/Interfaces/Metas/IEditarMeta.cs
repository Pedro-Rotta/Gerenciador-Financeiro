using GerenciadorContaFamiliar.Application.DTOs.Requests.Metas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using System.Threading;
using System.Threading.Tasks;
namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Metas;

public interface IEditarMetaUseCase
{
    Task<MetaResponse> ExecutarAsync(
        EditarMetaRequest request,
        CancellationToken cancellationToken = default);
}
