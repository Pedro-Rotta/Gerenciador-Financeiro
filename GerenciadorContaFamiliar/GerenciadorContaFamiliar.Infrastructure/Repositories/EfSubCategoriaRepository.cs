using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorContaFamiliar.Infrastructure.Repositories;

public class EfSubCategoriaRepository : ISubCategoriaRepository
{
    private readonly GerenciadorContaFamiliarDbContext _dbContext;

    public EfSubCategoriaRepository(GerenciadorContaFamiliarDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IReadOnlyList<SubCategoria>> ObterPorContaAsync(
        int contaId,
        CancellationToken ct = default)
    {
        IQueryable<SubCategoria> query = ComNavegacoes(_dbContext.SubCategorias);
        query = query.Where(t =>
            t.Ativo &&
            t.Conta != null && t.Conta.Id == contaId &&
            t.ContaConjunta == null);
            return await query.ToListAsync(ct);
    }
    public async Task<IReadOnlyList<SubCategoria>> ObterPorContaConjuntaAsync(
        int contaConjuntaId,
        CancellationToken ct = default)
    {
        IQueryable<SubCategoria> query = ComNavegacoes(_dbContext.SubCategorias);
        query = query.Where(t =>
            t.Ativo &&
            t.ContaConjunta != null && t.ContaConjunta.Id == contaConjuntaId);

        return await query.ToListAsync(ct);
    }

    private IQueryable<SubCategoria> ComNavegacoes(IQueryable<SubCategoria> query)
    {
        return query
            .AsNoTracking()
            .Include(s => s.Conta)
            .Include(s => s.ContaConjunta)
            .Include(s => s.Categorias);
    }

    public async Task<SubCategoria?> ObterPorIdAsync(int id, CancellationToken ct = default)
        => await _dbContext.SubCategorias.FindAsync(new object[] { id }, ct);

    public async Task<SubCategoria?> ObterPorNomeAsync(string nome, int? contaId, int? contaConjuntaId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return null;

        return await _dbContext.SubCategorias
            .AsNoTracking()
            .FirstOrDefaultAsync(s =>
                s.Nome == nome &&
                s.ContaId == contaId &&
                s.ContaConjuntaId == contaConjuntaId, ct);
    }

    public async Task<IReadOnlyList<SubCategoria>> ObterTodasAsync(CancellationToken ct = default)
        => await _dbContext.SubCategorias
            .AsNoTracking()
            .OrderBy(s => s.Nome)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SubCategoria>> ObterVinculadasAsync(int categoriaId, CancellationToken ct = default)
        => await _dbContext.SubCategorias
            .AsNoTracking()
            .Where(s => s.Categorias.Any(c => c.Id == categoriaId))
            .OrderBy(s => s.Nome)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SubCategoria>> ObterDisponiveisAsync(int categoriaId, CancellationToken ct = default)
        => await _dbContext.SubCategorias
            .AsNoTracking()
            .Where(s => !s.Categorias.Any(c => c.Id == categoriaId))
            .OrderBy(s => s.Nome)
            .ToListAsync(ct);

    public async Task AdicionarAsync(SubCategoria subCategoria, CancellationToken ct = default)
        => await _dbContext.SubCategorias.AddAsync(subCategoria, ct);

    public async Task AtualizarAsync(SubCategoria subCategoria, CancellationToken ct = default)
    {
        _dbContext.SubCategorias.Update(subCategoria);
        await Task.CompletedTask;
    }

    public async Task RemoverAsync(int id, CancellationToken ct = default)
    {
        var subCategoria = await _dbContext.SubCategorias.FindAsync(new object[] { id }, ct);
        if (subCategoria != null)
        {
            subCategoria.Inativar();
        }
        await Task.CompletedTask;
    }
}
