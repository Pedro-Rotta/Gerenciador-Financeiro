using GerenciadorContaFamiliar.Application.DTOs.Requests.Transacoes;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using System.Threading;
using System.Threading.Tasks;
namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Transacoes;

public interface IExcluirTransacaoUseCase
{
    Task<TransacaoResponse> ExecutarAsync(
        ExcluirTransacaoRequest request,
        CancellationToken cancellationToken = default);
}
