using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorContaFamiliar.Infrastructure.Repositories;

public class EfNotificacaoRepository : INotificacaoRepository
{
    private readonly GerenciadorContaFamiliarDbContext _dbContext;

    public EfNotificacaoRepository(GerenciadorContaFamiliarDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<Notificacao?> ObterPorIdAsync(int id, CancellationToken ct = default)
        => await _dbContext.Notificacoes
            .Include(n => n.Membro)
            .FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task<IReadOnlyList<Notificacao>> ObterPorMembroAsync(int membroId, CancellationToken ct = default)
        => await _dbContext.Notificacoes
            .AsNoTracking()
            .Where(n => n.Membro.Id == membroId)
            .ToListAsync(ct);

    public async Task<HashSet<string>> ObterChavesAsync(int membroId, string prefixo, CancellationToken ct = default)
    {
        var chaves = await _dbContext.Notificacoes
            .AsNoTracking()
            .Where(n => n.Membro.Id == membroId && n.Chave != null && n.Chave.StartsWith(prefixo))
            .Select(n => n.Chave!)
            .ToListAsync(ct);
        return chaves.ToHashSet();
    }

    public async Task AdicionarAsync(Notificacao notificacao, CancellationToken ct = default)
        => await _dbContext.Notificacoes.AddAsync(notificacao, ct);

    public async Task AtualizarAsync(Notificacao notificacao, CancellationToken ct = default)
    {
        _dbContext.Notificacoes.Update(notificacao);
        await Task.CompletedTask;
    }

    public async Task RemoverAsync(int id, CancellationToken ct = default)
    {
        var notificacao = await _dbContext.Notificacoes.FindAsync(new object[] { id }, ct);
        if (notificacao != null)
        {
            _dbContext.Notificacoes.Remove(notificacao);
        }
        await Task.CompletedTask;
    }
}
