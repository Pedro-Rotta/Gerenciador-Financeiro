using GerenciadorContaFamiliar.Application.DTOs.Requests.Notificacoes;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using System.Threading;
using System.Threading.Tasks;
namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Notificacoes;

public interface IEditarNotificacaoUseCase
{
    Task<NotificacaoResponse> ExecutarAsync(
        EditarNotificacaoRequest request,
        CancellationToken cancellationToken = default);
}
