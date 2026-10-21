using GerenciadorContaFamiliar.Application.DTOs.Requests.ContasConjuntas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using System.Threading;
using System.Threading.Tasks;
namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.ContasConjuntas;

public interface ICriarContaConjuntaUseCase
{
    Task<ContaConjuntaResponse> ExecutarAsync(
        CriarContaConjuntaRequest request, 
        CancellationToken cancellationToken = default);
}