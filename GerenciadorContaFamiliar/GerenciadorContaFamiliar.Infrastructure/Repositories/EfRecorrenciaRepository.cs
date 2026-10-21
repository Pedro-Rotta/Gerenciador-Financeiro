using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorContaFamiliar.Infrastructure.Repositories;

public class EfRecorrenciaRepository : IRecorrenciaRepository
{
    private readonly GerenciadorContaFamiliarDbContext _dbContext;

    public EfRecorrenciaRepository(GerenciadorContaFamiliarDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<Recorrencia?> ObterPorIdAsync(int id, CancellationToken ct = default)
        => await ComNavegacoes(_dbContext.Recorrencias)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<Recorrencia>> ObterAtivasPorMembroAsync(int membroId, CancellationToken ct = default)
        => await ComNavegacoes(_dbContext.Recorrencias)
            .Where(r => r.Ativa &&
                ((r.Conta != null && r.Conta.MembroId == membroId) ||
                 (r.ContaConjunta != null && r.ContaConjunta.Membros.Any(m => m.Id == membroId))))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Recorrencia>> ObterAtivasPorContaAsync(int contaId, CancellationToken ct = default)
        => await _dbContext.Recorrencias
            .Where(r => r.Ativa && EF.Property<int?>(r, "ContaId") == contaId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Recorrencia>> ObterAtivasPorContaConjuntaAsync(
        int contaConjuntaId, int? autorId = null, CancellationToken ct = default)
        => await _dbContext.Recorrencias
            .Where(r => r.Ativa &&
                EF.Property<int?>(r, "ContaConjuntaId") == contaConjuntaId &&
                (autorId == null || EF.Property<int>(r, "MembroId") == autorId))
            .ToListAsync(ct);

    public async Task AdicionarAsync(Recorrencia recorrencia, CancellationToken ct = default)
        => await _dbContext.Recorrencias.AddAsync(recorrencia, ct);

    public async Task AtualizarAsync(Recorrencia recorrencia, CancellationToken ct = default)
    {
        _dbContext.Recorrencias.Update(recorrencia);
        await Task.CompletedTask;
    }

    // Com rastreamento: a geração lança ocorrências na conta (saldo) e avança a série.
    // Conta, conjunta, categoria e subcategoria excluídas vêm nulas (filtro de soft delete).
    private static IQueryable<Recorrencia> ComNavegacoes(IQueryable<Recorrencia> query)
        => query
            .Include(r => r.Membro)
            .Include(r => r.Conta)
            .Include(r => r.ContaConjunta)
                .ThenInclude(cc => cc!.Membros)
            .Include(r => r.Categoria)
            .Include(r => r.SubCategoria);
}
