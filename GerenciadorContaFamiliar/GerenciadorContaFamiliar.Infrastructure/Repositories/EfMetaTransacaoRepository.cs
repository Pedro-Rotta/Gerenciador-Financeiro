using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorContaFamiliar.Infrastructure.Repositories;

/// <summary>
/// Implementação concreta do repositório de MetaTransacao usando Entity Framework Core.
/// Esta classe fica na camada de Infraestrutura e implementa o contrato definido no domínio.
/// </summary>
public class EfMetaTransacaoRepository : IMetaTransacaoRepository
{
    private readonly GerenciadorContaFamiliarDbContext _dbContext;

    public EfMetaTransacaoRepository(GerenciadorContaFamiliarDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<MetaTransacao?> ObterPorIdAsync(int id, CancellationToken ct = default)
    {
        return await _dbContext.MetasTransacoes
            .FirstOrDefaultAsync(mt => mt.Id == id, ct);
    }

    public async Task<IReadOnlyList<MetaTransacao>> ObterPorMetaAsync(int metaId, CancellationToken ct = default)
    {
        return await _dbContext.MetasTransacoes
            .AsNoTracking()
            .Where(mt => mt.MetaId == metaId)
            .ToListAsync(ct);
    }

    public async Task<bool> TransacaoJaVinculadaAsync(int transacaoId, CancellationToken ct = default)
    {
        return await _dbContext.MetasTransacoes
            .AnyAsync(mt => mt.TransacaoId == transacaoId, ct);
    }

    public async Task<MetaTransacao?> ObterPorTransacaoAsync(int transacaoId, CancellationToken ct = default)
    {
        return await _dbContext.MetasTransacoes
            .AsNoTracking()
            .FirstOrDefaultAsync(mt => mt.TransacaoId == transacaoId, ct);
    }

    public async Task<MetaTransacao?> ObterPorMetaETransacaoAsync(int metaId, int transacaoId, CancellationToken ct = default)
    {
        return await _dbContext.MetasTransacoes
            .FirstOrDefaultAsync(mt => mt.MetaId == metaId && mt.TransacaoId == transacaoId, ct);
    }

    public async Task AdicionarAsync(MetaTransacao metaTransacao, CancellationToken ct = default)
    {
        await _dbContext.MetasTransacoes.AddAsync(metaTransacao, ct);
    }

    public async Task RemoverAsync(int id, CancellationToken ct = default)
    {
        var metaTransacao = await _dbContext.MetasTransacoes.FindAsync(new object[] { id }, ct);
        if (metaTransacao != null)
        {
            _dbContext.MetasTransacoes.Remove(metaTransacao);
        }
        await Task.CompletedTask;
    }
}
