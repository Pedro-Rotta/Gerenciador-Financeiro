using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Metas;
using GerenciadorContaFamiliar.Application.UseCases.Notificacoes;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;

namespace GerenciadorContaFamiliar.Application.UseCases.Metas;

public class ProgressoDaMeta : IProgressoDaMeta
{
    // Marcos de progresso avisados ao dono da meta (do maior para o menor)
    private static readonly int[] MarcosPercentuais = { 75, 50, 25 };

    private readonly IMetaRepository _metaRepository;
    private readonly ITransacaoRepository _transacaoRepository;
    private readonly IMetaTransacaoRepository _metaTransacaoRepository;
    private readonly INotificacaoRepository _notificacaoRepository;
    private readonly IPreferenciaNotificacaoRepository _preferenciaRepository;

    public ProgressoDaMeta(
        IMetaRepository metaRepository,
        ITransacaoRepository transacaoRepository,
        IMetaTransacaoRepository metaTransacaoRepository,
        INotificacaoRepository notificacaoRepository,
        IPreferenciaNotificacaoRepository preferenciaRepository)
    {
        _metaRepository = metaRepository ?? throw new ArgumentNullException(nameof(metaRepository));
        _transacaoRepository = transacaoRepository ?? throw new ArgumentNullException(nameof(transacaoRepository));
        _metaTransacaoRepository = metaTransacaoRepository ?? throw new ArgumentNullException(nameof(metaTransacaoRepository));
        _notificacaoRepository = notificacaoRepository ?? throw new ArgumentNullException(nameof(notificacaoRepository));
        _preferenciaRepository = preferenciaRepository ?? throw new ArgumentNullException(nameof(preferenciaRepository));
    }

    public async Task RecalcularAsync(
        Meta meta,
        Transacao? incluir = null,
        int? ignorarTransacaoId = null,
        Membro? avisarDono = null,
        CancellationToken ct = default)
    {
        if (meta is null)
            throw new ArgumentNullException(nameof(meta));

        // A transação vem do contexto: se ela mudou nesta requisição (status, exclusão),
        // o valor considerado já é o novo
        decimal soma = 0;
        foreach (var vinculacao in await _metaTransacaoRepository.ObterPorMetaAsync(meta.Id, ct))
        {
            if (vinculacao.TransacaoId == ignorarTransacaoId || vinculacao.TransacaoId == incluir?.Id)
                continue;
            var transacao = await _transacaoRepository.ObterPorIdAsync(vinculacao.TransacaoId, ct);
            if (transacao is not null && ContaNaMeta(transacao))
                soma += transacao.Valor;
        }
        if (incluir is not null && ContaNaMeta(incluir))
            soma += incluir.Valor;

        var valorAnterior = meta.ValorAtual;
        var estavaConcluida = meta.Concluida;
        meta.AtualizarValorAtual(soma);

        if (avisarDono is null)
            return;
        var aviso = CriarAvisoDeProgresso(meta, avisarDono, valorAnterior, estavaConcluida);
        var preferencia = await _preferenciaRepository.ObterPorMembroAsync(avisarDono.Id, ct)
            ?? new PreferenciaNotificacao(avisarDono.Id);
        if (aviso is not null && preferencia.MetaProgresso)
            await _notificacaoRepository.AdicionarAsync(aviso, ct);
    }

    public async Task RecalcularDaTransacaoAsync(Transacao transacao, bool avisar, CancellationToken ct = default)
    {
        if (transacao is null)
            throw new ArgumentNullException(nameof(transacao));

        var vinculacao = await _metaTransacaoRepository.ObterPorTransacaoAsync(transacao.Id, ct);
        if (vinculacao is null)
            return;
        var meta = await _metaRepository.ObterPorIdAsync(vinculacao.MetaId, ct);
        if (meta is null || !meta.Ativo)
            return;

        await RecalcularAsync(meta, avisarDono: avisar ? transacao.Membro : null, ct: ct);
        await _metaRepository.AtualizarAsync(meta, ct);
    }

    // Só o que já foi pago/recebido conta na meta
    private static bool ContaNaMeta(Transacao transacao)
        => transacao.Ativo && transacao.Status == Transacao.StatusTransacao.CONCLUIDA;

    // Meta concluída agora, ou o maior marco (75, 50 ou 25%) ultrapassado agora; null se nenhum
    private static Notificacao? CriarAvisoDeProgresso(Meta meta, Membro dono, decimal valorAnterior, bool estavaConcluida)
    {
        if (meta.Concluida && !estavaConcluida)
            return new Notificacao(
                $"Meta \"{meta.Nome}\" concluída: você chegou a {AvisosNotificacao.Moeda(meta.ValorAlvo)}.",
                dono, Notificacao.TipoNotificacao.META, Notificacao.GravidadeNotificacao.INFORMATIVA);

        foreach (var marco in MarcosPercentuais)
        {
            var limite = meta.ValorAlvo * marco / 100;
            if (valorAnterior < limite && meta.ValorAtual >= limite)
                return new Notificacao(
                    $"A meta \"{meta.Nome}\" chegou a {marco}%: {AvisosNotificacao.Moeda(meta.ValorAtual)} de {AvisosNotificacao.Moeda(meta.ValorAlvo)}.",
                    dono, Notificacao.TipoNotificacao.META, Notificacao.GravidadeNotificacao.INFORMATIVA);
        }
        return null;
    }
}
