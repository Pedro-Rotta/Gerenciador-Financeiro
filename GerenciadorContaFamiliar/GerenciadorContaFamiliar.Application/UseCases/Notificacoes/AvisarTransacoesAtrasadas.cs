using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Verificacao;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;

namespace GerenciadorContaFamiliar.Application.UseCases.Notificacoes;

/// <summary>
/// Passo da verificação: PENDENTE com data anterior a hoje vira ATRASADA (sempre). Com o
/// interruptor ligado, avisa uma vez cada atrasada dos últimos <see cref="DiasParaAvisar"/> dias,
/// inclusive as que já estavam ATRASADA sem aviso.
/// </summary>
public class AvisarTransacoesAtrasadas : IPassoVerificacao
{
    /// <summary>Atrasadas mais antigas que isso só mudam de status (evita inundar o sino).</summary>
    public const int DiasParaAvisar = 30;
    private const string PrefixoChave = "atrasada:";

    private readonly ITransacaoRepository _transacaoRepository;
    private readonly IMembroRepository _membroRepository;
    private readonly INotificacaoRepository _notificacaoRepository;
    private readonly IPreferenciaNotificacaoRepository _preferenciaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AvisarTransacoesAtrasadas(
        ITransacaoRepository transacaoRepository,
        IMembroRepository membroRepository,
        INotificacaoRepository notificacaoRepository,
        IPreferenciaNotificacaoRepository preferenciaRepository,
        IUnitOfWork unitOfWork)
    {
        _transacaoRepository = transacaoRepository ?? throw new ArgumentNullException(nameof(transacaoRepository));
        _membroRepository = membroRepository ?? throw new ArgumentNullException(nameof(membroRepository));
        _notificacaoRepository = notificacaoRepository ?? throw new ArgumentNullException(nameof(notificacaoRepository));
        _preferenciaRepository = preferenciaRepository ?? throw new ArgumentNullException(nameof(preferenciaRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public string Nome => "Transações atrasadas";

    public async Task ExecutarAsync(int membroId, CancellationToken ct = default)
    {
        var membro = await _membroRepository.ObterPorIdAsync(membroId, ct);
        if (membro is null)
            return;

        var hoje = Relogio.HojeBrasilia();
        var atrasadas = (await _transacaoRepository.ObterPendentesEAtrasadasDoMembroAsync(membroId, ct))
            .Where(t => t.DataTransacao < hoje)
            .ToList();

        foreach (var transacao in atrasadas.Where(t => t.Status == Transacao.StatusTransacao.PENDENTE))
            transacao.AtualizarStatus(Transacao.StatusTransacao.ATRASADA);

        var preferencia = await _preferenciaRepository.ObterPorMembroAsync(membroId, ct)
            ?? new PreferenciaNotificacao(membroId);
        if (preferencia.Atrasada)
        {
            var jaAvisadas = await _notificacaoRepository.ObterChavesAsync(membroId, PrefixoChave, ct);
            foreach (var transacao in atrasadas.Where(t => t.DataTransacao >= hoje.AddDays(-DiasParaAvisar)))
            {
                var chave = PrefixoChave + transacao.Id;
                if (jaAvisadas.Contains(chave))
                    continue;
                await _notificacaoRepository.AdicionarAsync(new Notificacao(
                    $"A {AvisosNotificacao.DescreverLancamento(transacao)} está atrasada desde {transacao.DataTransacao:dd/MM/yyyy}.",
                    membro, Notificacao.TipoNotificacao.TRANSACAO, Notificacao.GravidadeNotificacao.CRITICA, chave), ct);
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
