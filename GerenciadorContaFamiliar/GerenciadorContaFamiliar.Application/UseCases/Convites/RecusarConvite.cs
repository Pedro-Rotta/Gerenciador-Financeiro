using GerenciadorContaFamiliar.Application.DTOs.Requests.Convites;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Convites;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.Exceptions;

namespace GerenciadorContaFamiliar.Application.UseCases.Convites;

/// <summary>
/// O convidado recusa o convite; quem convidou recebe uma notificação.
/// </summary>
public class RecusarConviteUseCase : IRecusarConviteUseCase
{
    private readonly IConviteContaConjuntaRepository _conviteRepository;
    private readonly INotificacaoRepository _notificacaoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RecusarConviteUseCase(
        IConviteContaConjuntaRepository conviteRepository,
        INotificacaoRepository notificacaoRepository,
        IUnitOfWork unitOfWork)
    {
        _conviteRepository = conviteRepository ?? throw new ArgumentNullException(nameof(conviteRepository));
        _notificacaoRepository = notificacaoRepository ?? throw new ArgumentNullException(nameof(notificacaoRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ConviteResponse> ExecutarAsync(
        RecusarConviteRequest request,
        CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // 1. Buscar o convite
        var convite = await _conviteRepository.ObterPorIdAsync(request.Id, ct)
            ?? throw new KeyNotFoundException("Convite não encontrado.");

        // 0. Verificar propriedade: só o convidado pode recusar
        if (convite.ConvidadoId != request.MembroId)
            throw new AcessoNegadoException("Você não tem permissão para responder este convite.");

        // 2. Recusar (o domínio garante que o convite ainda está pendente)
        convite.Recusar();

        // 3. Avisar quem convidou
        var notificacao = new Notificacao(
            $"{convite.Convidado.Nome} recusou o convite para a conta conjunta \"{convite.ContaConjunta.Nome}\".",
            convite.ConvidadoPor,
            Notificacao.TipoNotificacao.CONTA_CONJUNTA,
            Notificacao.GravidadeNotificacao.ATENCAO);

        // 4. Persistir
        await _conviteRepository.AtualizarAsync(convite, ct);
        await _notificacaoRepository.AdicionarAsync(notificacao, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // 5. Mapear resposta
        return MapearParaResponse(convite);
    }

    private static ConviteResponse MapearParaResponse(ConviteContaConjunta c) => new()
    {
        Id = c.Id,
        ContaConjuntaId = c.ContaConjuntaId,
        ContaConjuntaNome = c.ContaConjunta.Nome,
        ConvidadoId = c.ConvidadoId,
        ConvidadoNome = c.Convidado.Nome,
        ConvidadoNomeDeUsuario = c.Convidado.NomeDeUsuario,
        ConvidadoPorId = c.ConvidadoPorId,
        ConvidadoPorNome = c.ConvidadoPor.Nome,
        Status = c.Status,
        CriadoEm = c.CriadoEm,
        RespondidoEm = c.RespondidoEm,
    };
}
