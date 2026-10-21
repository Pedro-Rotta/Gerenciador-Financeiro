using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorContaFamiliar.Infrastructure.Repositories
{
    /// <summary>
    /// Implementação concreta do repositório de Transacao usando Entity Framework Core.
    /// Esta classe fica na camada de Infraestrutura e implementa o contrato definido no domínio.
    /// </summary>
    public class EfTransacaoRepository : ITransacaoRepository
    {
        // O repositório depende diretamente do DbContext (EF Core específico).
        // Isso é aceitável aqui porque estamos na camada de Infraestrutura –
        // o domínio nunca vê essa dependência.
        private readonly GerenciadorContaFamiliarDbContext _dbContext;

        public EfTransacaoRepository(GerenciadorContaFamiliarDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<Transacao?> ObterPorIdAsync(int id, CancellationToken ct = default)
        {
            // Conta/ContaConjunta são carregadas para permitir o estorno do saldo na exclusão;
            // os participantes da conjunta, para checar se o autor ainda faz parte dela
            return await _dbContext.Transacoes
                .Include(t => t.Membro)
                .Include(t => t.Conta)
                .Include(t => t.ContaConjunta)
                    .ThenInclude(cc => cc!.Membros)
                .Include(t => t.Recorrencia)
                .FirstOrDefaultAsync(t => t.Id == id, ct);
        }

        public async Task<IReadOnlyList<Transacao>> ObterPorContaAsync(
            int contaId,
            CancellationToken ct = default)
        {
            IQueryable<Transacao> query = ComNavegacoes(_dbContext.Transacoes);

            query = query.Where(t =>
                t.Ativo &&
                t.Conta != null && t.Conta.Id == contaId &&
                t.ContaConjunta == null);

            return await query.ToListAsync(ct);
        }
        public async Task<IReadOnlyList<Transacao>> ObterPorContaConjuntaAsync(
            int contaConjuntaId,
            CancellationToken ct = default)
        {
            IQueryable<Transacao> query = ComNavegacoes(_dbContext.Transacoes);
            query = query.Where(t =>
                t.Ativo &&
                t.ContaConjunta != null && t.ContaConjunta.Id == contaConjuntaId);

            return await query.ToListAsync(ct);
        }

        // Carrega as navegações necessárias para montar o TransacaoResponse
        // (a entidade não possui FKs escalares — depende destas navegações).
        private IQueryable<Transacao> ComNavegacoes(IQueryable<Transacao> query)
        {
            return query
                .AsNoTracking()
                .Include(t => t.Membro)
                .Include(t => t.Categoria)
                .Include(t => t.SubCategoria)
                .Include(t => t.Conta)
                .Include(t => t.ContaConjunta)
                .Include(t => t.Recorrencia);
        }

        public async Task<IReadOnlyList<Transacao>> ObterPorPeriodoAsync(
            DateTime dataInicio,
            DateTime dataFim,
            CancellationToken ct = default)
        {
            // Converte DateTime para DateOnly para comparação com DataTransacao (DateOnly)
            var dataInicioDateOnly = DateOnly.FromDateTime(dataInicio);
            var dataFimDateOnly = DateOnly.FromDateTime(dataFim);
            
            // Busca transações dentro do intervalo de datas (inclusive nos extremos)
            return await _dbContext.Transacoes
                .Where(t => t.DataTransacao >= dataInicioDateOnly && t.DataTransacao <= dataFimDateOnly)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<Transacao>> ObterParaRelatorioAsync(
            int membroId,
            int? idCategoria,
            int? idSubCategoria,
            bool apenasSemSubcategoria = false,
            DateOnly? dataInicio = null,
            DateOnly? dataFim = null,
            CancellationToken ct = default)
        {
            // Escopo obrigatório: só transações das contas do membro
            // (contas individuais dele + contas conjuntas das quais participa).
            IQueryable<Transacao> query = _dbContext.Transacoes
                .AsNoTracking()
                .Include(t => t.Categoria)
                .Include(t => t.SubCategoria)
                // Conta/ContaConjunta: nome da conta no PDF de transações
                .Include(t => t.Conta)
                .Include(t => t.ContaConjunta)
                .Where(t =>
                    (t.Conta != null && t.Conta.MembroId == membroId) ||
                    (t.ContaConjunta != null && t.ContaConjunta.Membros.Any(m => m.Id == membroId)));

            if (idCategoria.HasValue)
                query = query.Where(t => t.Categoria != null && t.Categoria.Id == idCategoria.Value);

            if (idSubCategoria.HasValue)
                query = query.Where(t => t.SubCategoria != null && t.SubCategoria.Id == idSubCategoria.Value);

            if (apenasSemSubcategoria)
                query = query.Where(t => t.SubCategoria == null);

            if (dataInicio.HasValue)
                query = query.Where(t => t.DataTransacao >= dataInicio.Value);

            if (dataFim.HasValue)
                query = query.Where(t => t.DataTransacao <= dataFim.Value);

            return await query
                .OrderByDescending(t => t.DataTransacao)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<Transacao>> ObterEmAbertoDaSerieAposAsync(
            int recorrenciaId,
            DateOnly data,
            CancellationToken ct = default)
        {
            // Com rastreamento: a exclusão estorna o saldo e inativa cada ocorrência
            return await _dbContext.Transacoes
                .Include(t => t.Conta)
                .Include(t => t.ContaConjunta)
                .Where(t =>
                    t.Ativo &&
                    t.Status != Transacao.StatusTransacao.CONCLUIDA &&
                    t.Recorrencia != null && t.Recorrencia.Id == recorrenciaId &&
                    t.DataTransacao > data)
                .ToListAsync(ct);
        }

        public async Task<decimal> SomarSaidasDaCategoriaAsync(
            int categoriaId,
            DateOnly dataInicio,
            DateOnly dataFim,
            CancellationToken ct = default)
        {
            bool NoFiltro(Transacao t) =>
                t.Ativo && t.Tipo == Transacao.TipoTransacao.SAIDA &&
                t.DataTransacao >= dataInicio && t.DataTransacao <= dataFim;

            var gravadas = await _dbContext.Transacoes
                .Where(t =>
                    t.Ativo && t.Tipo == Transacao.TipoTransacao.SAIDA &&
                    t.Categoria != null && t.Categoria.Id == categoriaId &&
                    t.DataTransacao >= dataInicio && t.DataTransacao <= dataFim)
                .SumAsync(t => t.Valor, ct);

            // Adicionadas nesta requisição (ex.: várias ocorrências geradas de uma vez)
            var naoGravadas = _dbContext.ChangeTracker.Entries<Transacao>()
                .Where(e => e.State == EntityState.Added && e.Entity.Categoria?.Id == categoriaId && NoFiltro(e.Entity))
                .Sum(e => e.Entity.Valor);

            return gravadas + naoGravadas;
        }

        public async Task<decimal> SomarEmAbertoAsync(int? contaId, int? contaConjuntaId, CancellationToken ct = default)
        {
            return await _dbContext.Transacoes
                .Where(t =>
                    t.Ativo &&
                    (t.Status == Transacao.StatusTransacao.PENDENTE || t.Status == Transacao.StatusTransacao.ATRASADA) &&
                    (contaId != null ? t.Conta != null && t.Conta.Id == contaId
                                     : t.ContaConjunta != null && t.ContaConjunta.Id == contaConjuntaId))
                .SumAsync(t => t.Tipo == Transacao.TipoTransacao.ENTRADA ? t.Valor : -t.Valor, ct);
        }

        public async Task<IReadOnlyList<Transacao>> ObterPendentesEAtrasadasDoMembroAsync(
            int membroId,
            CancellationToken ct = default)
        {
            return await _dbContext.Transacoes
                .Where(t =>
                    t.Ativo &&
                    (t.Status == Transacao.StatusTransacao.PENDENTE || t.Status == Transacao.StatusTransacao.ATRASADA) &&
                    ((t.Conta != null && t.Conta.MembroId == membroId) ||
                     (t.ContaConjunta != null && t.ContaConjunta.Membros.Any(m => m.Id == membroId))))
                .ToListAsync(ct);
        }

        public async Task AdicionarAsync(Transacao transacao, CancellationToken ct = default)
        {
            // EF Core: adiciona entidade ao contexto (será salva no SaveChangesAsync)
            await _dbContext.Transacoes.AddAsync(transacao, ct);
        }

        public async Task AtualizarAsync(Transacao transacao, CancellationToken ct = default)
        {
            // EF Core: marca entidade como modificada (Update é síncrono, mas retornamos Task.CompletedTask para manter assinatura assíncrona)
            _dbContext.Transacoes.Update(transacao);
            await Task.CompletedTask;
        }

        public async Task RemoverAsync(int id, CancellationToken ct = default)
        {
            // Busca a entidade primeiro (necessário para remover via EF Core)
            var transacao = await _dbContext.Transacoes.FindAsync(new object[] { id }, ct);
            if (transacao != null)
            {
                _dbContext.Transacoes.Remove(transacao);
            }
            await Task.CompletedTask;
        }
    }
}