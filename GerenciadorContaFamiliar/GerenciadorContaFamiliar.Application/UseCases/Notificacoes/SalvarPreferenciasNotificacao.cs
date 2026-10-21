using GerenciadorContaFamiliar.Application.DTOs.Requests.Notificacoes;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Notificacoes;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;

namespace GerenciadorContaFamiliar.Application.UseCases.Notificacoes;

public class SalvarPreferenciasNotificacaoUseCase : ISalvarPreferenciasNotificacaoUseCase
{
    private readonly IPreferenciaNotificacaoRepository _preferenciaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SalvarPreferenciasNotificacaoUseCase(
        IPreferenciaNotificacaoRepository preferenciaRepository,
        IUnitOfWork unitOfWork)
    {
        _preferenciaRepository = preferenciaRepository ?? throw new ArgumentNullException(nameof(preferenciaRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<PreferenciasNotificacaoResponse> ExecutarAsync(
        SalvarPreferenciasNotificacaoRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // A linha do membro nasce na primeira vez que ele salva
        var preferencia = await _preferenciaRepository.ObterPorMembroAsync(request.MembroId, ct);
        var nova = preferencia is null;
        preferencia ??= new PreferenciaNotificacao(request.MembroId);

        // O domínio valida: dias 1/3/7 e valor obrigatório com o saldo baixo ligado
        preferencia.Atualizar(
            request.LancamentosOutros,
            request.Vencimento,
            request.DiasAntesVencimento,
            request.Atrasada,
            request.SaldoBaixo,
            request.ValorSaldoBaixo,
            request.GastoAcimaNormal,
            request.MetaProgresso,
            request.MetaPrazo,
            request.HorarioSilencio);

        if (nova)
            await _preferenciaRepository.AdicionarAsync(preferencia, ct);
        else
            await _preferenciaRepository.AtualizarAsync(preferencia, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return ObterPreferenciasNotificacaoUseCase.Mapear(preferencia);
    }
}
