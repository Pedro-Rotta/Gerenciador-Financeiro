using GerenciadorContaFamiliar.Application.DTOs.Requests.Notificacoes;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Notificacoes;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;

namespace GerenciadorContaFamiliar.Application.UseCases.Notificacoes;

/// <summary>
/// Lista as notificações do membro logado (mais recentes primeiro).
/// </summary>
public class ListarNotificacaoUseCase : IListarNotificacaoUseCase
{
    private readonly INotificacaoRepository _notificacaoRepository;

    public ListarNotificacaoUseCase(INotificacaoRepository notificacaoRepository)
    {
        _notificacaoRepository = notificacaoRepository ?? throw new ArgumentNullException(nameof(notificacaoRepository));
    }

    public async Task<IEnumerable<NotificacaoResponse>> ExecutarAsync(
        ListarNotificacaoRequest request,
        CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var notificacoes = await _notificacaoRepository.ObterPorMembroAsync(request.MembroId, ct);

        return notificacoes
            .OrderByDescending(n => n.CriadoEm)
            .Select(n => new NotificacaoResponse
            {
                Id = n.Id,
                Mensagem = n.Mensagem,
                Lida = n.Lida,
                Tipo = n.Tipo,
                Gravidade = n.Gravidade,
                MembroId = request.MembroId,
                CriadoEm = n.CriadoEm
            });
    }
}
