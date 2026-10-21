using GerenciadorContaFamiliar.Application.DTOs.Requests.ContasConjuntas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using System.Threading;
using System.Threading.Tasks;
namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.ContasConjuntas;

public interface ISairContaConjuntaUseCase
{
    Task<ContaConjuntaResponse> ExecutarAsync(
        SairContaConjuntaRequest request,
        CancellationToken cancellationToken = default);
}
