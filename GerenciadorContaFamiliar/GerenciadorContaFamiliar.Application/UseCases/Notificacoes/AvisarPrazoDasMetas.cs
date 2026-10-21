using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Verificacao;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;

namespace GerenciadorContaFamiliar.Application.UseCases.Notificacoes;

/// <summary>
/// Passo da verificação: avisa uma vez cada meta ativa e não concluída do membro cujo prazo
/// está entre hoje e hoje + <see cref="DiasAntesDoPrazo"/> dias.
/// </summary>
public class AvisarPrazoDasMetas : IPassoVerificacao
{
    public const int DiasAntesDoPrazo = 30;
    private const string PrefixoChave = "prazo:";

    private readonly IMetaRepository _metaRepository;
    private readonly IMembroRepository _membroRepository;
    private readonly INotificacaoRepository _notificacaoRepository;
    private readonly IPreferenciaNotificacaoRepository _preferenciaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AvisarPrazoDasMetas(
        IMetaRepository metaRepository,
        IMembroRepository membroRepository,
        INotificacaoRepository notificacaoRepository,
        IPreferenciaNotificacaoRepository preferenciaRepository,
        IUnitOfWork unitOfWork)
    {
        _metaRepository = metaRepository ?? throw new ArgumentNullException(nameof(metaRepository));
        _membroRepository = membroRepository ?? throw new ArgumentNullException(nameof(membroRepository));
        _notificacaoRepository = notificacaoRepository ?? throw new ArgumentNullException(nameof(notificacaoRepository));
        _preferenciaRepository = preferenciaRepository ?? throw new ArgumentNullException(nameof(preferenciaRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public string Nome => "Prazo das metas";

    public async Task ExecutarAsync(int membroId, CancellationToken ct = default)
    {
        var preferencia = await _preferenciaRepository.ObterPorMembroAsync(membroId, ct)
            ?? new PreferenciaNotificacao(membroId);
        if (!preferencia.MetaPrazo)
            return;
        var membro = await _membroRepository.ObterPorIdAsync(membroId, ct);
        if (membro is null)
            return;

        var hoje = Relogio.HojeBrasilia();
        var limite = hoje.AddDays(DiasAntesDoPrazo);
        var metas = (await _metaRepository.ObterPorMembroAsync(membroId, ct))
            .Where(m => m.Ativo && !m.Concluida && m.Prazo >= hoje && m.Prazo <= limite);

        var jaAvisadas = await _notificacaoRepository.ObterChavesAsync(membroId, PrefixoChave, ct);
        foreach (var meta in metas)
        {
            var chave = PrefixoChave + meta.Id;
            if (jaAvisadas.Contains(chave))
                continue;
            var percentual = meta.ValorAlvo > 0 ? (int)Math.Floor(meta.ValorAtual / meta.ValorAlvo * 100) : 0;
            var progresso = $"ela está em {percentual}% ({AvisosNotificacao.Moeda(meta.ValorAtual)} de {AvisosNotificacao.Moeda(meta.ValorAlvo)})";
            await _notificacaoRepository.AdicionarAsync(new Notificacao(
                $"{DescreverPrazo(meta, hoje)} e {progresso}.",
                membro, Notificacao.TipoNotificacao.META, Notificacao.GravidadeNotificacao.ATENCAO, chave), ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    // "O prazo da meta "Viagem" é hoje", "Falta 1 dia para...", "Faltam 20 dias para..."
    private static string DescreverPrazo(Meta meta, DateOnly hoje)
    {
        var dias = meta.Prazo.DayNumber - hoje.DayNumber;
        return dias switch
        {
            0 => $"O prazo da meta \"{meta.Nome}\" é hoje",
            1 => $"Falta 1 dia para o prazo da meta \"{meta.Nome}\"",
            _ => $"Faltam {dias} dias para o prazo da meta \"{meta.Nome}\"",
        };
    }
}
