using GerenciadorContaFamiliar.Application.DTOs.Requests.Metas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using System.Threading;
using System.Threading.Tasks;
namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Metas;

public interface IExcluirMetaUseCase
{
    Task<MetaResponse> ExecutarAsync(
        ExcluirMetaRequest request,
        CancellationToken cancellationToken = default);
}
