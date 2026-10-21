using GerenciadorContaFamiliar.Application.DTOs.Requests.Contas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using System.Threading;
using System.Threading.Tasks;
namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Contas;

public interface IExcluirContaUseCase
{
    Task<ContaResponse> ExecutarAsync(
        ExcluirContaRequest request, 
        CancellationToken cancellationToken = default);
}