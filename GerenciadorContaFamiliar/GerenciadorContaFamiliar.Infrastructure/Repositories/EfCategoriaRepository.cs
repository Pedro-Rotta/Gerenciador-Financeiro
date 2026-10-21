using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorContaFamiliar.Infrastructure.Repositories;
public class EfCategoriaRepository : ICategoriaRepository
{
    private readonly GerenciadorContaFamiliarDbContext _dbContext;

    public EfCategoriaRepository(GerenciadorContaFamiliarDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }
    public async Task<Categoria?> ObterPorIdAsync(int id, CancellationToken ct = default)
    {
        return await _dbContext.Categorias.FindAsync(new object[] { id }, ct);
    }
    public async Task<Categoria?> ObterPorNomeAsync(string nome, int? contaId, int? contaConjuntaId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return null;
        return await _dbContext.Categorias
            .AsNoTracking()
            .FirstOrDefaultAsync(m =>
                m.Nome == nome &&
                m.ContaId == contaId &&
                m.ContaConjuntaId == contaConjuntaId, ct);
    }
    public async Task<IReadOnlyList<Categoria>> ObterTodasAsync(CancellationToken ct = default)
    {
        return await _dbContext.Categorias
            .AsNoTracking()
            .OrderBy(c => c.Nome)
            .ToListAsync(ct);
    }
    public async Task<Categoria?> ObterComSubCategoriasAsync(int id, CancellationToken ct = default)
    {
        return await _dbContext.Categorias
            .Include(c => c.SubCategorias)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }
    public async Task<IReadOnlyList<Categoria>> ObterPorTipoAsync(Categoria.TipoCategoria tipo, CancellationToken ct = default)
    {
        return await _dbContext.Categorias
            .AsNoTracking()
            .Where(c => c.Tipo == tipo)
            .ToListAsync(ct);
    }
    public async Task AdicionarAsync(Categoria categoria, CancellationToken ct = default)
    {
        await _dbContext.Categorias.AddAsync(categoria, ct);
    }
    public async Task AtualizarAsync(Categoria categoria, CancellationToken ct = default)
    {
        _dbContext.Categorias.Update(categoria);
        await Task.CompletedTask;
    }
    public async Task RemoverAsync(int id, CancellationToken ct = default)
    {
        var categoria = await _dbContext.Categorias.FindAsync(new object [] { id }, ct);
        if(categoria != null)
        {
            // Soft delete: inativa a categoria em vez de removê-la fisicamente.
            categoria.Inativar();
        }
        await Task.CompletedTask;
    }
    public async Task<IReadOnlyList<Categoria>> ObterPorContaAsync(int contaId, CancellationToken ct = default)
    {
        return await _dbContext.Categorias
            .Include(c => c.SubCategorias)
            .Where(c => c.ContaId == contaId && c.Ativo)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Categoria>> ObterPorContaConjuntaAsync(int contaConjuntaId, CancellationToken ct = default)
    {
        return await _dbContext.Categorias
            .Include(c => c.SubCategorias)
            .Where(c => c.ContaConjuntaId == contaConjuntaId && c.Ativo)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}