using GerenciadorContaFamiliar.Application.DTOs.Requests.ContasConjuntas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.ContasConjuntas;
using GerenciadorContaFamiliar.Application.Exceptions;

namespace GerenciadorContaFamiliar.Application.UseCases.ContasConjuntas;

/// <summary>
/// O criador remove um participante da conta conjunta; o removido recebe uma notificação.
/// As transações que ele lançou continuam no histórico da conta.
/// </summary>
public class RemoverParticipanteUseCase : IRemoverParticipanteUseCase
{
    private readonly IContaConjuntaRepository _contaConjuntaRepository;
    private readonly INotificacaoRepository _notificacaoRepository;
    private readonly IRecorrenciaRepository _recorrenciaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoverParticipanteUseCase(
        IContaConjuntaRepository contaConjuntaRepository,
        INotificacaoRepository notificacaoRepository,
        IRecorrenciaRepository recorrenciaRepository,
        IUnitOfWork unitOfWork)
    {
        _contaConjuntaRepository = contaConjuntaRepository ?? throw new ArgumentNullException(nameof(contaConjuntaRepository));
        _notificacaoRepository = notificacaoRepository ?? throw new ArgumentNullException(nameof(notificacaoRepository));
        _recorrenciaRepository = recorrenciaRepository ?? throw new ArgumentNullException(nameof(recorrenciaRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ContaConjuntaResponse> ExecutarAsync(
        RemoverParticipanteRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // 1. Buscar a conta conjunta (com os participantes)
        var contaConjunta = await _contaConjuntaRepository.ObterPorIdAsync(request.Id, ct)
            ?? throw new KeyNotFoundException("Conta conjunta não encontrada.");

        // 2. Verificar propriedade: só o criador pode remover participantes
        if (contaConjunta.CriadorId != request.MembroId)
            throw new AcessoNegadoException("Só o criador da conta conjunta pode remover participantes.");

        var participante = contaConjunta.Membros.FirstOrDefault(m => m.Id == request.ParticipanteId)
            ?? throw new KeyNotFoundException("Este membro não participa da conta conjunta.");

        // 3. Remover (o domínio impede remover o criador)
        contaConjunta.RemoverMembro(participante);

        // As séries recorrentes que ele criou nesta conta param de repetir
        foreach (var serie in await _recorrenciaRepository.ObterAtivasPorContaConjuntaAsync(contaConjunta.Id, participante.Id, ct))
            serie.Parar();

        // 4. Avisar quem foi removido
        var notificacao = new Notificacao(
            $"Você foi removido da conta conjunta \"{contaConjunta.Nome}\".",
            participante,
            Notificacao.TipoNotificacao.CONTA_CONJUNTA,
            Notificacao.GravidadeNotificacao.ATENCAO);

        // 5. Persistir tudo junto
        await _contaConjuntaRepository.AtualizarAsync(contaConjunta, ct);
        await _notificacaoRepository.AdicionarAsync(notificacao, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // 6. Mapear resposta
        return new ContaConjuntaResponse
        {
            Id = contaConjunta.Id,
            Nome = contaConjunta.Nome,
            Saldo = contaConjunta.Saldo,
            Descricao = contaConjunta.Descricao,
            CriadorId = contaConjunta.CriadorId,
            Ativo = contaConjunta.Ativo,
            Membros = contaConjunta.Membros
                .Select(m => new MembroResumoResponse { Id = m.Id, Nome = m.Nome, NomeDeUsuario = m.NomeDeUsuario })
                .ToList(),
        };
    }
}
