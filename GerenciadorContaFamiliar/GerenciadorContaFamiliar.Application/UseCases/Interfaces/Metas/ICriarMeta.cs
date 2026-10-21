using GerenciadorContaFamiliar.Application.DTOs.Requests.Metas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using System.Threading;
using System.Threading.Tasks;
namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Metas;

public interface ICriarMetaUseCase
{
    Task<MetaResponse> ExecutarAsync(
        CriarMetaRequest request,
        CancellationToken cancellationToken = default);
}
