using GerenciadorContaFamiliar.Application.DTOs.Requests.Membros;
using GerenciadorContaFamiliar.Application.DTOs.Responses;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Membros;

public interface IObterPreferenciasUsoUseCase
{
    /// <summary>Preferências de uso do membro (os padrões, se ele nunca salvou).</summary>
    Task<PreferenciasUsoResponse> ExecutarAsync(int membroId, CancellationToken ct = default);
}
