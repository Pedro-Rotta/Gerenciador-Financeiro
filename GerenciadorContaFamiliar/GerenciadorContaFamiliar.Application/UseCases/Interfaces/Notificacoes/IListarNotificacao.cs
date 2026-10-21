using GerenciadorContaFamiliar.Application.DTOs.Requests.Notificacoes;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Notificacoes;

public interface IListarNotificacaoUseCase
{
    Task<IEnumerable<NotificacaoResponse>> ExecutarAsync(
        ListarNotificacaoRequest request,
        CancellationToken ct = default);
}
