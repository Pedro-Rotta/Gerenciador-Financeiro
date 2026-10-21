using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Membros;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;

namespace GerenciadorContaFamiliar.Application.UseCases.Membros;

public class ObterPreferenciasUsoUseCase : IObterPreferenciasUsoUseCase
{
    private readonly IPreferenciaUsoRepository _preferenciaRepository;

    public ObterPreferenciasUsoUseCase(IPreferenciaUsoRepository preferenciaRepository)
    {
        _preferenciaRepository = preferenciaRepository ?? throw new ArgumentNullException(nameof(preferenciaRepository));
    }

    public async Task<PreferenciasUsoResponse> ExecutarAsync(int membroId, CancellationToken ct = default)
    {
        // Quem nunca salvou recebe os padrões (semana na segunda, conta principal, 1 mês, valores à mostra)
        var preferencia = await _preferenciaRepository.ObterPorMembroAsync(membroId, ct)
            ?? new PreferenciaUso(membroId);
        return Mapear(preferencia);
    }

    internal static PreferenciasUsoResponse Mapear(PreferenciaUso p) => new()
    {
        SemanaComecaNoDomingo = p.SemanaComecaNoDomingo,
        ContaPadrao = p.ContaPadrao,
        PeriodoRelatorios = p.PeriodoRelatorios,
        OcultarValores = p.OcultarValores,
    };
}
