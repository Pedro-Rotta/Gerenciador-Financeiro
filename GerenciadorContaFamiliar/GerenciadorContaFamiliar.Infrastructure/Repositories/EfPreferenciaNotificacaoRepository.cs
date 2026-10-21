using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorContaFamiliar.Infrastructure.Repositories;

public class EfPreferenciaNotificacaoRepository : IPreferenciaNotificacaoRepository
{
    private readonly GerenciadorContaFamiliarDbContext _dbContext;

    public EfPreferenciaNotificacaoRepository(GerenciadorContaFamiliarDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<PreferenciaNotificacao?> ObterPorMembroAsync(int membroId, CancellationToken ct = default)
        => await _dbContext.PreferenciasNotificacao
            .FirstOrDefaultAsync(p => p.MembroId == membroId, ct);

    public async Task<IReadOnlyList<PreferenciaNotificacao>> ObterPorMembrosAsync(
        IEnumerable<int> membroIds, CancellationToken ct = default)
    {
        var ids = membroIds.Distinct().ToList();
        return await _dbContext.PreferenciasNotificacao
            .AsNoTracking()
            .Where(p => ids.Contains(p.MembroId))
            .ToListAsync(ct);
    }

    public async Task AdicionarAsync(PreferenciaNotificacao preferencia, CancellationToken ct = default)
        => await _dbContext.PreferenciasNotificacao.AddAsync(preferencia, ct);

    public async Task AtualizarAsync(PreferenciaNotificacao preferencia, CancellationToken ct = default)
    {
        _dbContext.PreferenciasNotificacao.Update(preferencia);
        await Task.CompletedTask;
    }
}
