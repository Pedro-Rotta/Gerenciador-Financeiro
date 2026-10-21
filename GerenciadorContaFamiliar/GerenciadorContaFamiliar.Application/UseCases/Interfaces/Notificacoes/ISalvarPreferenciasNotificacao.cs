using GerenciadorContaFamiliar.Application.DTOs.Requests.Notificacoes;
using GerenciadorContaFamiliar.Application.DTOs.Responses;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Notificacoes;

public interface ISalvarPreferenciasNotificacaoUseCase
{
    Task<PreferenciasNotificacaoResponse> ExecutarAsync(
        SalvarPreferenciasNotificacaoRequest request,
        CancellationToken cancellationToken = default);
}
