using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorContaFamiliar.Infrastructure.Repositories;

public class EfMetaRepository : IMetaRepository
{
    private readonly GerenciadorContaFamiliarDbContext _dbContext;

    public EfMetaRepository(GerenciadorContaFamiliarDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<Meta?> ObterPorIdAsync(int id, CancellationToken ct = default)
        => await _dbContext.Metas.FindAsync(new object[] { id }, ct);

    public async Task<IReadOnlyList<Meta>> ObterPorMembroAsync(int membroId, CancellationToken ct = default)
        => await _dbContext.Metas
            .AsNoTracking()
            .Include(m => m.MetaTransacoes)
            .Where(m => m.MembroId == membroId)
            .ToListAsync(ct);

    public async Task<Meta?> ObterComTransacoesAsync(int id, CancellationToken ct = default)
        => await _dbContext.Metas
            .Include(m => m.MetaTransacoes)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task AdicionarAsync(Meta meta, CancellationToken ct = default)
        => await _dbContext.Metas.AddAsync(meta, ct);

    public async Task AtualizarAsync(Meta meta, CancellationToken ct = default)
    {
        _dbContext.Metas.Update(meta);
        await Task.CompletedTask;
    }

    public async Task RemoverAsync(int id, CancellationToken ct = default)
    {
        var meta = await _dbContext.Metas.FindAsync(new object[] { id }, ct);
        if (meta != null)
        {
            // Soft delete: inativa a meta em vez de removê-la fisicamente.
            meta.Inativar();
        }
        await Task.CompletedTask;
    }
}
