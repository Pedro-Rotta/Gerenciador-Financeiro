using GerenciadorContaFamiliar.Application.DTOs.Requests.Transacoes;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Metas;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Transacoes;
using GerenciadorContaFamiliar.Application.UseCases.Notificacoes;
using GerenciadorContaFamiliar.Application.Exceptions;

namespace GerenciadorContaFamiliar.Application.UseCases.Transacoes;

public class ExcluirTransacaoUseCase : IExcluirTransacaoUseCase
{
    private readonly ITransacaoRepository _transacaoRepository;
    private readonly INotificacaoRepository _notificacaoRepository;
    private readonly IPreferenciaNotificacaoRepository _preferenciaRepository;
    private readonly IProgressoDaMeta _progressoDaMeta;
    private readonly IUnitOfWork _unitOfWork;

    public ExcluirTransacaoUseCase(
        ITransacaoRepository transacaoRepository,
        INotificacaoRepository notificacaoRepository,
        IPreferenciaNotificacaoRepository preferenciaRepository,
        IProgressoDaMeta progressoDaMeta,
        IUnitOfWork unitOfWork)
    {
        _transacaoRepository = transacaoRepository ?? throw new ArgumentNullException(nameof(transacaoRepository));
        _notificacaoRepository = notificacaoRepository ?? throw new ArgumentNullException(nameof(notificacaoRepository));
        _preferenciaRepository = preferenciaRepository ?? throw new ArgumentNullException(nameof(preferenciaRepository));
        _progressoDaMeta = progressoDaMeta ?? throw new ArgumentNullException(nameof(progressoDaMeta));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<TransacaoResponse> ExecutarAsync(
        ExcluirTransacaoRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // 1. Buscar a entidade real pelo Id
        var transacao = await _transacaoRepository.ObterPorIdAsync(request.Id, ct)
            ?? throw new KeyNotFoundException("Transação não encontrada.");

        // 0. Verificar propriedade: só o autor pode excluir, e só enquanto participar da conta conjunta
        if (transacao.Membro.Id != request.MembroId ||
            (transacao.ContaConjunta is not null && !transacao.ContaConjunta.Membros.Any(m => m.Id == request.MembroId)))
            throw new AcessoNegadoException("Você não tem permissão para excluir esta transação.");

        if (!transacao.Ativo)
            throw new InvalidOperationException("Transação já está inativa.");

        var excluirProximas = request.Escopo == ExcluirTransacaoRequest.EscopoExclusao.PROXIMAS;
        if (excluirProximas && transacao.Recorrencia is null)
            throw new ArgumentException("Esta transação não faz parte de uma série recorrente.");

        // 2. Estornar o valor no saldo da conta e fazer o soft delete (inativa).
        // "Só esta" numa série não recria a data: a série já passou dela.
        transacao.EstornarDaConta();
        transacao.Inativar();

        // "Esta e as próximas": também as seguintes ainda não pagas (pendentes ou atrasadas); a série para
        if (excluirProximas)
        {
            var proximas = await _transacaoRepository.ObterEmAbertoDaSerieAposAsync(
                transacao.Recorrencia!.Id, transacao.DataTransacao, ct);
            foreach (var ocorrencia in proximas)
            {
                ocorrencia.EstornarDaConta();
                ocorrencia.Inativar();
            }
            transacao.Recorrencia.Parar();
        }

        // Vinculada a uma meta: a meta sai sem ela (só a transação principal pode estar
        // concluída; as próximas da série estão em aberto e não contam na meta)
        await _progressoDaMeta.RecalcularDaTransacaoAsync(transacao, avisar: false, ct);

        // Avisar os outros participantes da conta conjunta
        if (transacao.ContaConjunta is not null)
            await AvisosNotificacao.AvisarOutrosParticipantesSobreLancamentoAsync(
                _notificacaoRepository, _preferenciaRepository, transacao.ContaConjunta, transacao.Membro.Id,
                excluirProximas
                    ? $"{transacao.Membro.Nome} excluiu a {AvisosNotificacao.DescreverLancamento(transacao)} e as próximas da série em \"{transacao.ContaConjunta.Nome}\"; ela não se repete mais."
                    : $"{transacao.Membro.Nome} excluiu a {AvisosNotificacao.DescreverLancamento(transacao)} em \"{transacao.ContaConjunta.Nome}\".",
                Notificacao.GravidadeNotificacao.ATENCAO, ct);

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
