using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Notificacoes;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;

namespace GerenciadorContaFamiliar.Application.UseCases.Notificacoes;

public class AvisosDeLancamento : IAvisosDeLancamento
{
    /// <summary>Gasto do mês acima de 130% da média dos 3 meses anteriores.</summary>
    private const decimal FatorGastoAcimaNormal = 1.3m;
    private const int MesesDaMedia = 3;

    private readonly INotificacaoRepository _notificacaoRepository;
    private readonly IPreferenciaNotificacaoRepository _preferenciaRepository;
    private readonly ITransacaoRepository _transacaoRepository;

    // Chaves já avisadas nesta requisição e ainda não gravadas (ex.: várias ocorrências
    // geradas de uma vez na mesma categoria e mês): evita repetir dentro do mesmo SaveChanges
    private readonly HashSet<(int MembroId, string Chave)> _chavesDaRequisicao = new();

    public AvisosDeLancamento(
        INotificacaoRepository notificacaoRepository,
        IPreferenciaNotificacaoRepository preferenciaRepository,
        ITransacaoRepository transacaoRepository)
    {
        _notificacaoRepository = notificacaoRepository ?? throw new ArgumentNullException(nameof(notificacaoRepository));
        _preferenciaRepository = preferenciaRepository ?? throw new ArgumentNullException(nameof(preferenciaRepository));
        _transacaoRepository = transacaoRepository ?? throw new ArgumentNullException(nameof(transacaoRepository));
    }

    public async Task AvisarAsync(
        Transacao transacao,
        decimal saldoAnterior,
        bool geradaPelaSerie,
        CancellationToken ct = default)
    {
        if (transacao is null)
            throw new ArgumentNullException(nameof(transacao));

        var contaConjunta = transacao.ContaConjunta;
        var membro = transacao.Membro;

        // Destinatários dos avisos de saldo e gasto: o dono ou todos os participantes da conjunta
        var destinatarios = Destinatarios(transacao);
        var preferencias = await AvisosNotificacao.PreferenciasDosMembrosAsync(
            _preferenciaRepository, destinatarios.Select(m => m.Id), ct);

        // Avisar os outros participantes da conta conjunta (só no lançamento manual)
        if (!geradaPelaSerie && contaConjunta is not null)
            await AvisosNotificacao.AvisarOutrosParticipantesSobreLancamentoAsync(
                _notificacaoRepository, _preferenciaRepository, contaConjunta, membro.Id,
                $"{membro.Nome} lançou uma {AvisosNotificacao.DescreverLancamento(transacao)} em \"{contaConjunta.Nome}\".",
                Notificacao.GravidadeNotificacao.INFORMATIVA, ct);

        await AvisarSaldoAsync(transacao, saldoAnterior, destinatarios, preferencias, ct);

        var interessadosNoGasto = destinatarios.Where(d => preferencias[d.Id].GastoAcimaNormal).ToList();
        if (interessadosNoGasto.Count > 0)
            await AvisarGastoAcimaDoNormalAsync(transacao, interessadosNoGasto, ct);

        // Lembrar o autor de categorizar o lançamento (só no lançamento manual)
        if (!geradaPelaSerie && transacao.Categoria is null)
            await _notificacaoRepository.AdicionarAsync(new Notificacao(
                $"A {AvisosNotificacao.DescreverLancamento(transacao)} foi salva sem categoria.",
                membro, Notificacao.TipoNotificacao.TRANSACAO, Notificacao.GravidadeNotificacao.ATENCAO), ct);
    }

    public async Task AvisarSaldoAsync(Transacao transacao, decimal saldoAnterior, CancellationToken ct = default)
    {
        if (transacao is null)
            throw new ArgumentNullException(nameof(transacao));

        var destinatarios = Destinatarios(transacao);
        var preferencias = await AvisosNotificacao.PreferenciasDosMembrosAsync(
            _preferenciaRepository, destinatarios.Select(m => m.Id), ct);
        await AvisarSaldoAsync(transacao, saldoAnterior, destinatarios, preferencias, ct);
    }

    // O dono da conta ou todos os participantes da conjunta
    private static List<Membro> Destinatarios(Transacao transacao)
        => transacao.ContaConjunta?.Membros.ToList() ?? new List<Membro> { transacao.Membro };

    private async Task AvisarSaldoAsync(
        Transacao transacao,
        decimal saldoAnterior,
        List<Membro> destinatarios,
        IReadOnlyDictionary<int, PreferenciaNotificacao> preferencias,
        CancellationToken ct)
    {
        var nomeConta = transacao.Conta?.Nome ?? transacao.ContaConjunta!.Nome;
        var saldoAtual = transacao.Conta?.Saldo ?? transacao.ContaConjunta!.Saldo;

        // Saldo negativo: sempre avisa, para todos os destinatários
        if (saldoAnterior >= 0 && saldoAtual < 0)
        {
            var mensagem = $"O saldo da conta \"{nomeConta}\" ficou negativo: {AvisosNotificacao.Moeda(saldoAtual)}.";
            foreach (var destinatario in destinatarios)
                await _notificacaoRepository.AdicionarAsync(new Notificacao(
                    mensagem, destinatario, Notificacao.TipoNotificacao.SALDO, Notificacao.GravidadeNotificacao.CRITICA), ct);
        }

        // Saldo baixo: cada um com o próprio limite. Se ficou negativo, só o aviso acima
        if (saldoAtual >= 0)
            foreach (var destinatario in destinatarios)
            {
                var preferencia = preferencias[destinatario.Id];
                if (!preferencia.SaldoBaixo || preferencia.ValorSaldoBaixo is not decimal limite)
                    continue;
                if (saldoAnterior >= limite && saldoAtual < limite)
                    await _notificacaoRepository.AdicionarAsync(new Notificacao(
                        $"O saldo da conta \"{nomeConta}\" ficou abaixo de {AvisosNotificacao.Moeda(limite)}: {AvisosNotificacao.Moeda(saldoAtual)}.",
                        destinatario, Notificacao.TipoNotificacao.SALDO, Notificacao.GravidadeNotificacao.ATENCAO), ct);
            }
    }

    // Saída com categoria, do mês atual: total do mês na categoria (já com ela) acima de 130% da
    // média dos 3 meses-calendário anteriores (meses sem gasto contam zero; exige gasto em ao
    // menos um deles). Uma vez por categoria e mês para cada membro.
    private async Task AvisarGastoAcimaDoNormalAsync(Transacao transacao, List<Membro> destinatarios, CancellationToken ct)
    {
        if (transacao.EhEntrada() || transacao.Categoria is null)
            return;

        // Só o mês atual (Brasília): lançar histórico ou conta futura não avisa
        var hoje = Relogio.HojeBrasilia();
        if (transacao.DataTransacao.Year != hoje.Year || transacao.DataTransacao.Month != hoje.Month)
            return;

        var categoria = transacao.Categoria;
        var inicioDoMes = new DateOnly(hoje.Year, hoje.Month, 1);
        var totalDoMes = await _transacaoRepository.SomarSaidasDaCategoriaAsync(
            categoria.Id, inicioDoMes, inicioDoMes.AddMonths(1).AddDays(-1), ct);

        decimal somaAnteriores = 0;
        for (var i = 1; i <= MesesDaMedia; i++)
        {
            var inicio = inicioDoMes.AddMonths(-i);
            somaAnteriores += await _transacaoRepository.SomarSaidasDaCategoriaAsync(
                categoria.Id, inicio, inicio.AddMonths(1).AddDays(-1), ct);
        }
        if (somaAnteriores <= 0)
            return;
        var media = somaAnteriores / MesesDaMedia;
        if (totalDoMes <= media * FatorGastoAcimaNormal)
            return;

        var percentual = Math.Round((totalDoMes / media - 1) * 100);
        var mensagem = $"Gastos em \"{categoria.Nome}\" este mês: {AvisosNotificacao.Moeda(totalDoMes)}, " +
                       $"{percentual}% acima da média dos últimos 3 meses ({AvisosNotificacao.Moeda(media)}).";
        var chave = $"gasto:{categoria.Id}:{inicioDoMes:yyyy-MM}";

        foreach (var destinatario in destinatarios)
        {
            if (!_chavesDaRequisicao.Add((destinatario.Id, chave)))
                continue;
            var jaAvisadas = await _notificacaoRepository.ObterChavesAsync(destinatario.Id, chave, ct);
            if (jaAvisadas.Contains(chave))
                continue;
            await _notificacaoRepository.AdicionarAsync(new Notificacao(
                mensagem, destinatario, Notificacao.TipoNotificacao.TRANSACAO,
                Notificacao.GravidadeNotificacao.ATENCAO, chave), ct);
        }
    }
}
