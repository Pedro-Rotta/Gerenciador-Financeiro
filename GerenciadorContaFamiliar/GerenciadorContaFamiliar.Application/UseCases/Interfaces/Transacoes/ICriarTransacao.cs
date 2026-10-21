using GerenciadorContaFamiliar.Application.DTOs.Requests.Transacoes;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using System.Threading;
using System.Threading.Tasks;
namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Transacoes;

public interface ICriarTransacaoUseCase
{
    Task<TransacaoResponse> ExecutarAsync(
        CriarTransacaoRequest request,
        CancellationToken cancellationToken = default);
}
