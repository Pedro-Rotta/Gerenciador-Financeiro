using GerenciadorContaFamiliar.Application.DTOs.Requests.ContasConjuntas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.ContasConjuntas;
using GerenciadorContaFamiliar.Application.UseCases.Notificacoes;
using GerenciadorContaFamiliar.Application.Exceptions;

namespace GerenciadorContaFamiliar.Application.UseCases.ContasConjuntas;
public class ExcluirContaConjuntaUseCase : IExcluirContaConjuntaUseCase
{
    private readonly IContaConjuntaRepository _contaConjuntaRepository;
    private readonly INotificacaoRepository _notificacaoRepository;
    private readonly IRecorrenciaRepository _recorrenciaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ExcluirContaConjuntaUseCase(
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
        ExcluirContaConjuntaRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // 1. Buscar a entidade real pelo Id
        var contaConjunta = await _contaConjuntaRepository.ObterPorIdAsync(request.Id, ct)
            ?? throw new KeyNotFoundException("ContaConjunta não encontrada.");

        // 2. Verificar propriedade: só o criador pode excluir
        if (contaConjunta.CriadorId != request.MembroId)
            throw new AcessoNegadoException("Você não tem permissão para excluir esta conta conjunta.");

        if (!contaConjunta.Ativo)
            throw new InvalidOperationException("ContaConjunta já está inativa.");

        // 2. Soft delete (inativa)
        contaConjunta.Inativar();

        // As séries recorrentes da conta param de repetir
        foreach (var serie in await _recorrenciaRepository.ObterAtivasPorContaConjuntaAsync(contaConjunta.Id, ct: ct))
            serie.Parar();

        // Avisar os participantes que a conta foi encerrada
        var criador = contaConjunta.Membros.First(m => m.Id == contaConjunta.CriadorId);
        await AvisosNotificacao.AvisarOutrosParticipantesAsync(
            _notificacaoRepository, contaConjunta, criador.Id,
            $"{criador.Nome} encerrou a conta conjunta \"{contaConjunta.Nome}\".",
            Notificacao.GravidadeNotificacao.CRITICA, ct);

        // 3. Persistir
        await _contaConjuntaRepository.AtualizarAsync(contaConjunta, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // 4. Mapear resposta
        return new ContaConjuntaResponse
        {
            Id = contaConjunta.Id,
            Nome = contaConjunta.Nome,
            Saldo = contaConjunta.Saldo,
            Descricao = contaConjunta.Descricao,
            CriadorId = contaConjunta.CriadorId,
            Ativo = contaConjunta.Ativo
        };
    }
}
