using GerenciadorContaFamiliar.Application.DTOs.Requests.Notificacoes;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Notificacoes;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;

namespace GerenciadorContaFamiliar.Application.UseCases.Notificacoes;

public class ObterPreferenciasNotificacaoUseCase : IObterPreferenciasNotificacaoUseCase
{
    private readonly IPreferenciaNotificacaoRepository _preferenciaRepository;

    public ObterPreferenciasNotificacaoUseCase(IPreferenciaNotificacaoRepository preferenciaRepository)
    {
        _preferenciaRepository = preferenciaRepository ?? throw new ArgumentNullException(nameof(preferenciaRepository));
    }

    public async Task<PreferenciasNotificacaoResponse> ExecutarAsync(
        ObterPreferenciasNotificacaoRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // Quem nunca salvou recebe os padrões
        var preferencia = await _preferenciaRepository.ObterPorMembroAsync(request.MembroId, ct)
            ?? new PreferenciaNotificacao(request.MembroId);
        return Mapear(preferencia);
    }

    internal static PreferenciasNotificacaoResponse Mapear(PreferenciaNotificacao p) => new()
    {
        LancamentosOutros = p.LancamentosOutros,
        Vencimento = p.Vencimento,
        DiasAntesVencimento = p.DiasAntesVencimento,
        Atrasada = p.Atrasada,
        SaldoBaixo = p.SaldoBaixo,
        ValorSaldoBaixo = p.ValorSaldoBaixo,
        GastoAcimaNormal = p.GastoAcimaNormal,
        MetaProgresso = p.MetaProgresso,
        MetaPrazo = p.MetaPrazo,
        HorarioSilencio = p.HorarioSilencio,
    };
}
