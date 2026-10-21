using GerenciadorContaFamiliar.Application.DTOs.Requests.Transacoes;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Metas;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Notificacoes;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Transacoes;
using GerenciadorContaFamiliar.Application.UseCases.Notificacoes;
using GerenciadorContaFamiliar.Application.Exceptions;

namespace GerenciadorContaFamiliar.Application.UseCases.Transacoes;

public class EditarTransacaoUseCase : IEditarTransacaoUseCase
{
    private readonly ITransacaoRepository _transacaoRepository;
    private readonly INotificacaoRepository _notificacaoRepository;
    private readonly IPreferenciaNotificacaoRepository _preferenciaRepository;
    private readonly IProgressoDaMeta _progressoDaMeta;
    private readonly IAvisosDeLancamento _avisosDeLancamento;
    private readonly IUnitOfWork _unitOfWork;

    public EditarTransacaoUseCase(
        ITransacaoRepository transacaoRepository,
        INotificacaoRepository notificacaoRepository,
        IPreferenciaNotificacaoRepository preferenciaRepository,
        IProgressoDaMeta progressoDaMeta,
        IAvisosDeLancamento avisosDeLancamento,
        IUnitOfWork unitOfWork)
    {
        _transacaoRepository = transacaoRepository ?? throw new ArgumentNullException(nameof(transacaoRepository));
        _notificacaoRepository = notificacaoRepository ?? throw new ArgumentNullException(nameof(notificacaoRepository));
        _preferenciaRepository = preferenciaRepository ?? throw new ArgumentNullException(nameof(preferenciaRepository));
        _progressoDaMeta = progressoDaMeta ?? throw new ArgumentNullException(nameof(progressoDaMeta));
        _avisosDeLancamento = avisosDeLancamento ?? throw new ArgumentNullException(nameof(avisosDeLancamento));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<TransacaoResponse> ExecutarAsync(
        EditarTransacaoRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // 1. Buscar a entidade real pelo Id
        var transacao = await _transacaoRepository.ObterPorIdAsync(request.Id, ct)
            ?? throw new KeyNotFoundException("Transação não encontrada.");

        // 0. Verificar propriedade: só o autor pode editar, e só enquanto participar da conta conjunta
        if (transacao.Membro.Id != request.MembroId ||
            (transacao.ContaConjunta is not null && !transacao.ContaConjunta.Membros.Any(m => m.Id == request.MembroId)))
            throw new AcessoNegadoException("Você não tem permissão para editar esta transação.");

        // 2. Atualizar somente o que foi enviado
        var descricaoAnterior = transacao.Descricao;
        var statusAnterior = transacao.Status;
        // Conta excluída chega nula (filtro de inativos): editar só a descrição continua possível
        var saldoAnterior = transacao.Conta?.Saldo ?? transacao.ContaConjunta?.Saldo;
        if (request.Descricao is not null)
            transacao.AtualizarDescricao(request.Descricao);
        if (request.Status is not null)
            transacao.AtualizarStatus(request.Status.Value);

        if (transacao.Status != statusAnterior)
        {
            // Pagar ("Conta paga") ou desfazer muda o saldo: avisos de saldo baixo/negativo
            var saldoAtual = transacao.Conta?.Saldo ?? transacao.ContaConjunta?.Saldo;
            if (saldoAnterior is decimal anterior && saldoAtual != anterior)
                await _avisosDeLancamento.AvisarSaldoAsync(transacao, anterior, ct);

            // Vinculada a uma meta: só a concluída conta, então pagar (ou desfazer) muda o progresso
            await _progressoDaMeta.RecalcularDaTransacaoAsync(transacao, avisar: true, ct);
        }

        // Avisar os outros participantes da conta conjunta (só se algo mudou de fato)
        if (transacao.ContaConjunta is not null &&
            (transacao.Descricao != descricaoAnterior || transacao.Status != statusAnterior))
            await AvisosNotificacao.AvisarOutrosParticipantesSobreLancamentoAsync(
                _notificacaoRepository, _preferenciaRepository, transacao.ContaConjunta, transacao.Membro.Id,
                $"{transacao.Membro.Nome} editou a {AvisosNotificacao.DescreverLancamento(transacao)} em \"{transacao.ContaConjunta.Nome}\".",
                Notificacao.GravidadeNotificacao.INFORMATIVA, ct);

        // 3. Persistir
        await _transacaoRepository.AtualizarAsync(transacao, ct);
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