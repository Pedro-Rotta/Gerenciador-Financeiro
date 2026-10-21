using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorContaFamiliar.Infrastructure.Repositories;

public class EfCodigoRecuperacaoSenhaRepository : ICodigoRecuperacaoSenhaRepository
{
    private readonly GerenciadorContaFamiliarDbContext _dbContext;

    public EfCodigoRecuperacaoSenhaRepository(GerenciadorContaFamiliarDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<CodigoRecuperacaoSenha?> ObterUltimoDoMembroAsync(int membroId, CancellationToken ct = default)
    {
        return await _dbContext.CodigosRecuperacaoSenha
            .AsNoTracking()
            .Where(c => c.MembroId == membroId && !c.Usado)
            .OrderByDescending(c => c.Id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<int> ContarCriadosDesdeAsync(int membroId, DateTime desdeUtc, CancellationToken ct = default)
    {
        return await _dbContext.CodigosRecuperacaoSenha
            .CountAsync(c => c.MembroId == membroId && c.CriadoEm >= desdeUtc, ct);
    }

    public async Task<bool> ReservarTentativaAsync(int codigoId, DateTime agoraUtc, CancellationToken ct = default)
    {
        var afetados = await _dbContext.CodigosRecuperacaoSenha
            .Where(c => c.Id == codigoId && !c.Usado &&
                        c.Tentativas < CodigoRecuperacaoSenha.TentativasMaximas && c.ExpiraEm > agoraUtc)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Tentativas, c => c.Tentativas + 1), ct);
        return afetados == 1;
    }

    public async Task<bool> MarcarUsadoSeAbertoAsync(int codigoId, CancellationToken ct = default)
    {
        var afetados = await _dbContext.CodigosRecuperacaoSenha
            .Where(c => c.Id == codigoId && !c.Usado)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Usado, true), ct);
        return afetados == 1;
    }

    public async Task InvalidarDoMembroAsync(int membroId, CancellationToken ct = default)
    {
        var abertos = await _dbContext.CodigosRecuperacaoSenha
            .Where(c => c.MembroId == membroId && !c.Usado)
            .ToListAsync(ct);
        foreach (var codigo in abertos)
            codigo.MarcarUsado();
    }

    public async Task AdicionarAsync(CodigoRecuperacaoSenha codigo, CancellationToken ct = default)
    {
        await _dbContext.CodigosRecuperacaoSenha.AddAsync(codigo, ct);
    }
}
