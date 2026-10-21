using GerenciadorContaFamiliar.Application.DTOs.Requests.Notificacoes;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using System.Threading;
using System.Threading.Tasks;
namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Notificacoes;

public interface IExcluirNotificacaoUseCase
{
    Task<NotificacaoResponse> ExecutarAsync(
        ExcluirNotificacaoRequest request,
        CancellationToken cancellationToken = default);
}
