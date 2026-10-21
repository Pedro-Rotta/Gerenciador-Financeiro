using GerenciadorContaFamiliar.Application.DTOs.Requests.Contas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using System.Threading;
using System.Threading.Tasks;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Contas;

public interface IAlterarContaPrincipalUseCase
{
    Task<ContaResponse> ExecutarAsync(
        AlterarContaPrincipalRequest request,
        CancellationToken cancellationToken = default);
}
