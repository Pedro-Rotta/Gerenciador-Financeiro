using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorContaFamiliar.Infrastructure.Repositories;

public class EfPreferenciaUsoRepository : IPreferenciaUsoRepository
{
    private readonly GerenciadorContaFamiliarDbContext _dbContext;

    public EfPreferenciaUsoRepository(GerenciadorContaFamiliarDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<PreferenciaUso?> ObterPorMembroAsync(int membroId, CancellationToken ct = default)
    {
        return await _dbContext.PreferenciasUso.FirstOrDefaultAsync(p => p.MembroId == membroId, ct);
    }

    public async Task AdicionarAsync(PreferenciaUso preferencia, CancellationToken ct = default)
    {
        await _dbContext.PreferenciasUso.AddAsync(preferencia, ct);
    }
}
