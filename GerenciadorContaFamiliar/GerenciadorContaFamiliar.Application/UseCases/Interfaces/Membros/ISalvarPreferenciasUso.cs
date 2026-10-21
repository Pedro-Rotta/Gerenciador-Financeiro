using GerenciadorContaFamiliar.Application.DTOs.Requests.Membros;
using GerenciadorContaFamiliar.Application.DTOs.Responses;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Membros;

public interface ISalvarPreferenciasUsoUseCase
{
    /// <summary>Grava as preferências de uso do membro (cria na primeira vez).</summary>
    Task<PreferenciasUsoResponse> ExecutarAsync(SalvarPreferenciasUsoRequest request, CancellationToken ct = default);
}
