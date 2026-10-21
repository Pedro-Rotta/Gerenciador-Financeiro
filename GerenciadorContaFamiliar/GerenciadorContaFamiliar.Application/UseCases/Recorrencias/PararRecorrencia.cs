using GerenciadorContaFamiliar.Application.DTOs.Requests.Recorrencias;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Application.Exceptions;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Recorrencias;
using GerenciadorContaFamiliar.Application.UseCases.Notificacoes;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;

namespace GerenciadorContaFamiliar.Application.UseCases.Recorrencias;

/// <summary>
/// Para de repetir a série de uma ocorrência; as ocorrências já geradas ficam como estão.
/// </summary>
public class PararRecorrenciaUseCase : IPararRecorrenciaUseCase
{
    private readonly ITransacaoRepository _transacaoRepository;
    private readonly IRecorrenciaRepository _recorrenciaRepository;
    private readonly INotificacaoRepository _notificacaoRepository;
    private readonly IPreferenciaNotificacaoRepository _preferenciaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PararRecorrenciaUseCase(
        ITransacaoRepository transacaoRepository,
        IRecorrenciaRepository recorrenciaRepository,
        INotificacaoRepository notificacaoRepository,
        IPreferenciaNotificacaoRepository preferenciaRepository,
        IUnitOfWork unitOfWork)
    {
        _transacaoRepository = transacaoRepository ?? throw new ArgumentNullException(nameof(transacaoRepository));
        _recorrenciaRepository = recorrenciaRepository ?? throw new ArgumentNullException(nameof(recorrenciaRepository));
        _notificacaoRepository = notificacaoRepository ?? throw new ArgumentNullException(nameof(notificacaoRepository));
        _preferenciaRepository = preferenciaRepository ?? throw new ArgumentNullException(nameof(preferenciaRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<TransacaoResponse> ExecutarAsync(
        PararRecorrenciaRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // 1. Buscar a ocorrência (com a série)
        var transacao = await _transacaoRepository.ObterPorIdAsync(request.TransacaoId, ct)
            ?? throw new KeyNotFoundException("Transação não encontrada.");

        // 0. Verificar propriedade: só o autor da série, e só enquanto participar da conta conjunta
        if (transacao.Membro.Id != request.MembroId ||
            (transacao.ContaConjunta is not null && !transacao.ContaConjunta.Membros.Any(m => m.Id == request.MembroId)))
            throw new AcessoNegadoException("Você não tem permissão para alterar esta série.");

        var serie = transacao.Recorrencia
            ?? throw new ArgumentException("Esta transação não faz parte de uma série recorrente.");
        if (!serie.Ativa)
            throw new InvalidOperationException("Esta série já não repete mais.");

        // 2. Parar a série
        serie.Parar();

        // Avisar os outros participantes da conta conjunta
        if (transacao.ContaConjunta is not null)
            await AvisosNotificacao.AvisarOutrosParticipantesSobreLancamentoAsync(
                _notificacaoRepository, _preferenciaRepository, transacao.ContaConjunta, transacao.Membro.Id,
                $"{transacao.Membro.Nome} parou de repetir a {AvisosNotificacao.DescreverLancamento(transacao)} em \"{transacao.ContaConjunta.Nome}\".",
                Notificacao.GravidadeNotificacao.INFORMATIVA, ct);

        // 3. Persistir
        await _recorrenciaRepository.AtualizarAsync(serie, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // 4. Mapear resposta
        return new TransacaoResponse
        {
            Id = transacao.Id,
            Valor = transacao.Valor,
            Descricao = transacao.Descricao,
            DataTransacao = transacao.DataTransacao,
            Tipo = transacao.Tipo,
            Classificacao = transacao.Classificacao,
            Recorrente = transacao.Recorrente,
            Periodicidade = transacao.Periodicidade,
            Status = transacao.Status,
            MembroId = transacao.Membro.Id,
            CategoriaId = transacao.Categoria?.Id,
            SubCategoriaId = transacao.SubCategoria?.Id,
            ContaId = transacao.Conta?.Id,
            ContaConjuntaId = transacao.ContaConjunta?.Id,
            RecorrenciaId = transacao.Recorrencia?.Id,
            NumeroOcorrencia = transacao.NumeroOcorrencia,
            TotalOcorrencias = transacao.Recorrencia?.TotalOcorrencias,
            SerieAtiva = transacao.Recorrencia?.Ativa,
            CriadoEm = transacao.CriadoEm,
            Ativo = transacao.Ativo
        };
    }
}
