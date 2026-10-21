using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorContaFamiliar.Infrastructure.Repositories;
public class EfContaConjuntaRepository : IContaConjuntaRepository
{
    private readonly GerenciadorContaFamiliarDbContext _dbContext;

    public EfContaConjuntaRepository(GerenciadorContaFamiliarDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }
    public async Task<ContaConjunta?> ObterPorIdAsync(int id, CancellationToken ct = default)
    {
        // Com rastreamento: editar/excluir grava só a conta conjunta, e lançar transação
        // nela reutiliza o membro já carregado (sem tentar reinserir a conta/membros).
        return await _dbContext.ContasConjuntas
            .Include(cc => cc.Membros)
            .FirstOrDefaultAsync(cc => cc.Id == id, ct);
    }
    public async Task<IReadOnlyList<Membro>> ObterMembrosDaContaConjuntaAsync(int contaId, CancellationToken ct = default)
    {
        return await _dbContext.ContasConjuntas
            .AsNoTracking()
            .Where(cc => cc.Id == contaId)
            .SelectMany(cc => cc.Membros)
            .ToListAsync(ct);
    }
    public async Task<IReadOnlyList<ContaConjunta>> ObterPorMembroAsync(int membroId, CancellationToken ct = default)
    {
        if (membroId <= 0)
            return Array.Empty<ContaConjunta>();

        return await _dbContext.ContasConjuntas
            .AsNoTracking()
            .Include(cc => cc.Membros)
            .Where(cc => cc.Ativo && (cc.CriadorId == membroId || cc.Membros.Any(m => m.Id == membroId)))
            .ToListAsync(ct);
    }
    public async Task AdicionarAsync(ContaConjunta conta, CancellationToken ct = default)
    {
        await _dbContext.ContasConjuntas.AddAsync(conta, ct);
    }
    public async Task AtualizarAsync(ContaConjunta conta, CancellationToken ct = default)
    {
        _dbContext.ContasConjuntas.Update(conta);
        await Task.CompletedTask;
    }
    public async Task RemoverAsync(int id, CancellationToken ct = default)
    {
        var conta = await _dbContext.ContasConjuntas.FindAsync(new object [] { id }, ct);
        if(conta != null)
        {
            // Soft delete: inativa a conta conjunta em vez de removê-la fisicamente.
            conta.Inativar();
        }
        await Task.CompletedTask;
    }
}