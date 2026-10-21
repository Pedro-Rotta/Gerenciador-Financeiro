using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorContaFamiliar.Infrastructure.Repositories;

public class EfLogAuditoriaRepository : ILogAuditoriaRepository
{
    private readonly GerenciadorContaFamiliarDbContext _dbContext;

    public EfLogAuditoriaRepository(GerenciadorContaFamiliarDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<LogAuditoria?> ObterPorIdAsync(int id, CancellationToken ct = default)
        => await _dbContext.LogsAuditoria.FindAsync(new object[] { id }, ct);

    public async Task<IReadOnlyList<LogAuditoria>> ObterPorMembroAsync(int idMembro, CancellationToken ct = default)
        => await _dbContext.LogsAuditoria
            .AsNoTracking()
            .Where(l => l.IdMembro == idMembro)
            .ToListAsync(ct);

    public async Task AdicionarAsync(LogAuditoria log, CancellationToken ct = default)
        => await _dbContext.LogsAuditoria.AddAsync(log, ct);
}
