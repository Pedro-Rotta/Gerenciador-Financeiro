using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorContaFamiliar.Infrastructure.Repositories;
public class EfContaRepository : IContaRepository
{
    private readonly GerenciadorContaFamiliarDbContext _dbContext;

    public EfContaRepository(GerenciadorContaFamiliarDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }
    public async Task<Conta?> ObterPorIdAsync(int id, CancellationToken ct = default)
    {
        return await _dbContext.Contas.FindAsync(new object[] { id }, ct);
    }
    public async Task<Conta?> ObterPorNomeAsync(string nome, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return null;

        return await _dbContext.Contas
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Nome == nome, ct);
    }
    public async Task<Conta?> ObterContaPrincipalAsync(int membroId, CancellationToken ct = default)
    {
        if (membroId <= 0)
            return null;

        return await _dbContext.Contas
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.MembroId == membroId && c.ContaPrincipal && c.Ativo, ct);
    }
    public async Task<IReadOnlyList<Conta>> ObterPorMembroAsync(int membroId, CancellationToken ct = default)
    {
        if (membroId <= 0)
            return Array.Empty<Conta>();

        return await _dbContext.Contas
            .Where(c => c.MembroId == membroId && c.Ativo)
            .AsNoTracking()
            .ToListAsync(ct);
    }
    public async Task AdicionarAsync(Conta conta, CancellationToken ct = default)
    {
        await _dbContext.Contas.AddAsync(conta, ct);
    }
    public async Task AtualizarAsync(Conta conta, CancellationToken ct = default)
    {
        _dbContext.Contas.Update(conta);
        await Task.CompletedTask;
    }
    public async Task RemoverAsync(int id, CancellationToken ct = default)
    {
        var conta = await _dbContext.Contas.FindAsync(new object [] { id }, ct);
        if(conta != null)
        {
            conta.Inativar();
        }
        await Task.CompletedTask;
    }
}