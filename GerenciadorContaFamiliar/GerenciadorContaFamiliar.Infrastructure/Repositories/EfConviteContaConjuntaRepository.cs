using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorContaFamiliar.Infrastructure.Repositories;

public class EfConviteContaConjuntaRepository : IConviteContaConjuntaRepository
{
    private readonly GerenciadorContaFamiliarDbContext _dbContext;

    public EfConviteContaConjuntaRepository(GerenciadorContaFamiliarDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    // Com rastreamento: usado para aceitar, recusar ou cancelar o convite.
    public async Task<ConviteContaConjunta?> ObterPorIdAsync(int id, CancellationToken ct = default)
        => await _dbContext.ConvitesContasConjuntas
            .Include(c => c.ContaConjunta)
            .Include(c => c.Convidado)
            .Include(c => c.ConvidadoPor)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<ConviteContaConjunta>> ObterPendentesDoConvidadoAsync(int convidadoId, CancellationToken ct = default)
        => await _dbContext.ConvitesContasConjuntas
            .AsNoTracking()
            .Include(c => c.ContaConjunta)
            .Include(c => c.Convidado)
            .Include(c => c.ConvidadoPor)
            .Where(c => c.ConvidadoId == convidadoId && c.Status == ConviteContaConjunta.StatusConvite.PENDENTE)
            .OrderByDescending(c => c.CriadoEm)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ConviteContaConjunta>> ObterPendentesDaContaAsync(int contaConjuntaId, CancellationToken ct = default)
        => await _dbContext.ConvitesContasConjuntas
            .AsNoTracking()
            .Include(c => c.ContaConjunta)
            .Include(c => c.Convidado)
            .Include(c => c.ConvidadoPor)
            .Where(c => c.ContaConjuntaId == contaConjuntaId && c.Status == ConviteContaConjunta.StatusConvite.PENDENTE)
            .OrderByDescending(c => c.CriadoEm)
            .ToListAsync(ct);

    public async Task<bool> ExistePendenteAsync(int contaConjuntaId, int convidadoId, CancellationToken ct = default)
        => await _dbContext.ConvitesContasConjuntas
            .AnyAsync(c =>
                c.ContaConjuntaId == contaConjuntaId &&
                c.ConvidadoId == convidadoId &&
                c.Status == ConviteContaConjunta.StatusConvite.PENDENTE, ct);

    public async Task AdicionarAsync(ConviteContaConjunta convite, CancellationToken ct = default)
        => await _dbContext.ConvitesContasConjuntas.AddAsync(convite, ct);

    public async Task AtualizarAsync(ConviteContaConjunta convite, CancellationToken ct = default)
    {
        _dbContext.ConvitesContasConjuntas.Update(convite);
        await Task.CompletedTask;
    }
}
