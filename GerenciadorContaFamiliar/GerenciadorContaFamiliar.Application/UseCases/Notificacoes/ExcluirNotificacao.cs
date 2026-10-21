using GerenciadorContaFamiliar.Application.DTOs.Requests.Notificacoes;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Notificacoes;
using GerenciadorContaFamiliar.Application.Exceptions;

namespace GerenciadorContaFamiliar.Application.UseCases.Notificacoes;

public class ExcluirNotificacaoUseCase : IExcluirNotificacaoUseCase
{
    private readonly INotificacaoRepository _notificacaoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ExcluirNotificacaoUseCase(INotificacaoRepository notificacaoRepository, IUnitOfWork unitOfWork)
    {
        _notificacaoRepository = notificacaoRepository ?? throw new ArgumentNullException(nameof(notificacaoRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<NotificacaoResponse> ExecutarAsync(
        ExcluirNotificacaoRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // 1. Buscar a entidade real pelo Id
        var notificacao = await _notificacaoRepository.ObterPorIdAsync(request.Id, ct)
            ?? throw new KeyNotFoundException("Notificação não encontrada.");

        // 0. Verificar propriedade: só o dono pode excluir
        if (notificacao.Membro.Id != request.MembroId)
            throw new AcessoNegadoException("Você não tem permissão para excluir esta notificação.");

        // 2. Remover (hard delete)
        await _notificacaoRepository.RemoverAsync(notificacao.Id, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // 3. Mapear resposta
        return new NotificacaoResponse
        {
            Id = notificacao.Id,
            Mensagem = notificacao.Mensagem,
            Lida = notificacao.Lida,
            Tipo = notificacao.Tipo,
            Gravidade = notificacao.Gravidade,
            MembroId = notificacao.Membro.Id,
            CriadoEm = notificacao.CriadoEm
        };
    }
}
