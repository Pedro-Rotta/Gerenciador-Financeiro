using GerenciadorContaFamiliar.Application.DTOs.Requests.Notificacoes;
using GerenciadorContaFamiliar.Application.DTOs.Responses;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Notificacoes;

public interface IObterPreferenciasNotificacaoUseCase
{
    Task<PreferenciasNotificacaoResponse> ExecutarAsync(
        ObterPreferenciasNotificacaoRequest request,
        CancellationToken cancellationToken = default);
}
