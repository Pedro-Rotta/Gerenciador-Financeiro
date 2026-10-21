using GerenciadorContaFamiliar.Application.DTOs.Requests.Convites;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Convites;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.Exceptions;

namespace GerenciadorContaFamiliar.Application.UseCases.Convites;

/// <summary>
/// O convidado aceita o convite: passa a participar da conta conjunta (sem segundo aceite)
/// e quem convidou recebe uma notificação.
/// </summary>
public class AceitarConviteUseCase : IAceitarConviteUseCase
{
    private readonly IConviteContaConjuntaRepository _conviteRepository;
    private readonly IContaConjuntaRepository _contaConjuntaRepository;
    private readonly IMembroRepository _membroRepository;
    private readonly INotificacaoRepository _notificacaoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AceitarConviteUseCase(
        IConviteContaConjuntaRepository conviteRepository,
        IContaConjuntaRepository contaConjuntaRepository,
        IMembroRepository membroRepository,
        INotificacaoRepository notificacaoRepository,
        IUnitOfWork unitOfWork)
    {
        _conviteRepository = conviteRepository ?? throw new ArgumentNullException(nameof(conviteRepository));
        _contaConjuntaRepository = contaConjuntaRepository ?? throw new ArgumentNullException(nameof(contaConjuntaRepository));
        _membroRepository = membroRepository ?? throw new ArgumentNullException(nameof(membroRepository));
        _notificacaoRepository = notificacaoRepository ?? throw new ArgumentNullException(nameof(notificacaoRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ConviteResponse> ExecutarAsync(
        AceitarConviteRequest request,
        CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // 1. Buscar o convite
        var convite = await _conviteRepository.ObterPorIdAsync(request.Id, ct)
            ?? throw new KeyNotFoundException("Convite não encontrado.");

        // 0. Verificar propriedade: só o convidado pode aceitar
        if (convite.ConvidadoId != request.MembroId)
            throw new AcessoNegadoException("Você não tem permissão para responder este convite.");

        // 2. Aceitar (o domínio garante que o convite ainda está pendente)
        convite.Aceitar();

        // 3. Incluir o convidado como participante da conta conjunta
        var contaConjunta = await _contaConjuntaRepository.ObterPorIdAsync(convite.ContaConjuntaId, ct)
            ?? throw new KeyNotFoundException("Conta conjunta não encontrada.");
        var membro = await _membroRepository.ObterPorIdAsync(request.MembroId, ct)
            ?? throw new KeyNotFoundException("Membro não encontrado.");

        contaConjunta.AdicionarMembro(membro);

        // 4. Avisar quem convidou
        var notificacao = new Notificacao(
            $"{membro.Nome} aceitou o convite para a conta conjunta \"{contaConjunta.Nome}\".",
            convite.ConvidadoPor,
            Notificacao.TipoNotificacao.CONTA_CONJUNTA,
            Notificacao.GravidadeNotificacao.INFORMATIVA);

        // 5. Persistir tudo junto
        await _conviteRepository.AtualizarAsync(convite, ct);
        await _notificacaoRepository.AdicionarAsync(notificacao, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // 6. Mapear resposta
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
