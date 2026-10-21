using GerenciadorContaFamiliar.Domain;

namespace GerenciadorContaFamiliar.Domain.Shared.Interfaces;

public interface IPreferenciaUsoRepository
{
    /// <summary>Preferências de uso do membro (rastreadas), ou null se ele nunca salvou.</summary>
    Task<PreferenciaUso?> ObterPorMembroAsync(int membroId, CancellationToken ct = default);

    Task AdicionarAsync(PreferenciaUso preferencia, CancellationToken ct = default);
}
