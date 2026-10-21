using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Verificacao;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;

namespace GerenciadorContaFamiliar.Application.UseCases.Notificacoes;

/// <summary>
/// Passo da verificação: lembra uma vez cada PENDENTE que vence entre hoje e hoje + os dias
/// escolhidos (1, 3 ou 7). Roda depois da geração das séries, para alcançar as ocorrências novas.
/// </summary>
public class LembrarVencimentos : IPassoVerificacao
{
    private const string PrefixoChave = "vencimento:";

    private readonly ITransacaoRepository _transacaoRepository;
    private readonly IMembroRepository _membroRepository;
    private readonly INotificacaoRepository _notificacaoRepository;
    private readonly IPreferenciaNotificacaoRepository _preferenciaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LembrarVencimentos(
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

    public string Nome => "Lembrete de vencimento";

    public async Task ExecutarAsync(int membroId, CancellationToken ct = default)
    {
        var preferencia = await _preferenciaRepository.ObterPorMembroAsync(membroId, ct)
            ?? new PreferenciaNotificacao(membroId);
        if (!preferencia.Vencimento)
            return;
        var membro = await _membroRepository.ObterPorIdAsync(membroId, ct);
        if (membro is null)
            return;

        var hoje = Relogio.HojeBrasilia();
        var limite = hoje.AddDays(preferencia.DiasAntesVencimento);
        var aVencer = (await _transacaoRepository.ObterPendentesEAtrasadasDoMembroAsync(membroId, ct))
            .Where(t => t.Status == Transacao.StatusTransacao.PENDENTE && t.DataTransacao >= hoje && t.DataTransacao <= limite);

        var jaAvisadas = await _notificacaoRepository.ObterChavesAsync(membroId, PrefixoChave, ct);
        foreach (var transacao in aVencer)
        {
            var chave = PrefixoChave + transacao.Id;
            if (jaAvisadas.Contains(chave))
                continue;
            await _notificacaoRepository.AdicionarAsync(new Notificacao(
                $"A {AvisosNotificacao.DescreverLancamento(transacao)} {DescreverVencimento(transacao.DataTransacao, hoje)}.",
                membro, Notificacao.TipoNotificacao.TRANSACAO, Notificacao.GravidadeNotificacao.ATENCAO, chave), ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    // "vence hoje", "vence amanhã (10/10/2026)", "vence em 3 dias (12/10/2026)"
    private static string DescreverVencimento(DateOnly data, DateOnly hoje)
    {
        var dias = data.DayNumber - hoje.DayNumber;
        return dias switch
        {
            0 => "vence hoje",
            1 => $"vence amanhã ({data:dd/MM/yyyy})",
            _ => $"vence em {dias} dias ({data:dd/MM/yyyy})",
        };
    }
}
