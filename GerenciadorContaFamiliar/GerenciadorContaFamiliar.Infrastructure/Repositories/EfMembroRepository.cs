using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorContaFamiliar.Infrastructure.Repositories;
public class EfMembroRepository : IMembroRepository
{
    private readonly GerenciadorContaFamiliarDbContext _dbContext;

    public EfMembroRepository(GerenciadorContaFamiliarDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }
    public async Task<Membro?> ObterPorIdAsync(int id, CancellationToken ct = default)
    {
        return await _dbContext.Membros.FindAsync(new object[] { id }, ct);
    }
    public async Task<Membro?> ObterPorNomeDeUsuarioAsync(string nomeDeUsuario, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nomeDeUsuario))
            return null;

        return await _dbContext.Membros
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.NomeDeUsuario == nomeDeUsuario, ct);
    }
    public async Task<Membro?> ObterPorEmailAsync(string email, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        // E-mail não diferencia maiúsculas: compara em minúsculas (lower() no PostgreSQL)
        var emailNormalizado = email.Trim().ToLower();

        return await _dbContext.Membros
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Email.ToLower() == emailNormalizado, ct);
    }
    public async Task<Membro?> ObterPorTelefoneAsync(string? telefone, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(telefone))
            return null;

        return await _dbContext.Membros
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Telefone == telefone, ct);
    }
    public async Task AdicionarAsync(Membro membro, CancellationToken ct = default)
    {
        await _dbContext.Membros.AddAsync(membro, ct);
    }
    public async Task AtualizarAsync(Membro membro, CancellationToken ct = default)
    {
        _dbContext.Membros.Update(membro);
        await Task.CompletedTask;
    }
    public async Task RemoverAsync(int id, CancellationToken ct = default)
    {
        var membro = await _dbContext.Membros
            .FirstOrDefaultAsync(m => m.Id == id, ct);
        if(membro != null)
        {
            // Soft delete: inativa o membro em vez de removê-lo fisicamente.
            membro.Inativar();
        }
        await Task.CompletedTask;
    }
}