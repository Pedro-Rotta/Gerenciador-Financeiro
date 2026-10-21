using GerenciadorContaFamiliar.Application.DTOs.Requests.Contas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using System.Threading;
using System.Threading.Tasks;
namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Contas;

public interface ICriarContaUseCase
{
    Task<ContaResponse> ExecutarAsync(
        CriarContaRequest request, 
        CancellationToken cancellationToken = default);
}