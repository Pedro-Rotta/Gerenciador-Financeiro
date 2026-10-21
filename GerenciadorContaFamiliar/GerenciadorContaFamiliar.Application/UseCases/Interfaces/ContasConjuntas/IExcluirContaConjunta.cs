using GerenciadorContaFamiliar.Application.DTOs.Requests.ContasConjuntas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using System.Threading;
using System.Threading.Tasks;
namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.ContasConjuntas;

public interface IExcluirContaConjuntaUseCase
{
    Task<ContaConjuntaResponse> ExecutarAsync(
        ExcluirContaConjuntaRequest request, 
        CancellationToken cancellationToken = default);
}