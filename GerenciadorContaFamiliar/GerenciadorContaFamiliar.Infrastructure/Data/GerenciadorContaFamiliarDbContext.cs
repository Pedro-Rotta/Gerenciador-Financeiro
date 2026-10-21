using System.Linq.Expressions;
using System.Text.Json;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;

namespace GerenciadorContaFamiliar.Infrastructure.Data
{
    public class GerenciadorContaFamiliarDbContext : DbContext, IUnitOfWork
    {
        private IDbContextTransaction? _transacaoAtual;
        private readonly IUsuarioAtual? _usuarioAtual;

        // Campos que nunca devem ser gravados no log de auditoria.
        private static readonly HashSet<string> CamposSensiveis = new()
        {
            nameof(Membro.SenhaHash),
            nameof(CodigoRecuperacaoSenha.CodigoHash),
        };

        public GerenciadorContaFamiliarDbContext(
            DbContextOptions<GerenciadorContaFamiliarDbContext> options,
            IUsuarioAtual? usuarioAtual = null)
            : base(options)
        {
            _usuarioAtual = usuarioAtual;
        }

        public DbSet<Transacao> Transacoes { get; set; } = null!;
        public DbSet<Membro> Membros { get; set; } = null!;
        public DbSet<Conta> Contas { get; set; } = null!;
        public DbSet<Categoria> Categorias { get; set; } = null!;
        public DbSet<SubCategoria> SubCategorias { get; set; } = null!;
        public DbSet<ContaConjunta> ContasConjuntas { get; set; } = null!;
        public DbSet<LogAuditoria> LogsAuditoria { get; set; } = null!;
        public DbSet<Meta> Metas { get; set; } = null!;
        public DbSet<Notificacao> Notificacoes { get; set; } = null!;
        public DbSet<MetaTransacao> MetasTransacoes { get; set; } = null!;
        public DbSet<ConviteContaConjunta> ConvitesContasConjuntas { get; set; } = null!;
        public DbSet<Recorrencia> Recorrencias { get; set; } = null!;
        public DbSet<PreferenciaNotificacao> PreferenciasNotificacao { get; set; } = null!;
        public DbSet<CodigoRecuperacaoSenha> CodigosRecuperacaoSenha { get; set; } = null!;
        public DbSet<PreferenciaUso> PreferenciasUso { get; set; } = null!;

        #region IUnitOfWork (transações explícitas)

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transacaoAtual != null)
                throw new InvalidOperationException("Já existe uma transação em andamento no contexto atual.");

            _transacaoAtual = await Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_transacaoAtual == null)
                throw new InvalidOperationException("Nenhuma transação em andamento para confirmar.");

            await _transacaoAtual.CommitAsync(cancellationToken);
            await _transacaoAtual.DisposeAsync();
            _transacaoAtual = null;
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_transacaoAtual == null)
                throw new InvalidOperationException("Nenhuma transação em andamento para desfazer.");

            await _transacaoAtual.RollbackAsync(cancellationToken);
            await _transacaoAtual.DisposeAsync();
            _transacaoAtual = null;
        }

        public void DescartarAlteracoes() => ChangeTracker.Clear();

        #endregion

        #region Auditoria automática

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Captura ANTES de salvar: assim temos os valores ORIGINAIS (dados antigos)
            // das entidades modificadas/deletadas. Para INSERT, o Id ainda não existe.
            var auditoriaPendente = new List<(EntityEntry Entry, LogAuditoria.OperacaoLog Operacao, string? DadosAntigos, string? DadosNovos)>();

            foreach (var entry in ChangeTracker.Entries())
            {
                // Audita apenas entidades de domínio. Tabelas de junção
                // (ex.: CategoriaSubCategoria) são Dictionary<string,object> e
                // não têm Id — devem ser ignoradas pela auditoria.
                if (entry.Entity is not BaseEntity)
                    continue;

                if (entry.Entity is LogAuditoria)   // evita loop infinito auditando a própria auditoria
                    continue;

                switch (entry.State)
                {
                    case EntityState.Added:
                        auditoriaPendente.Add((entry, LogAuditoria.OperacaoLog.INSERT,
                            null, SerializarValores(entry.CurrentValues)));
                        break;
                    case EntityState.Modified:
                        auditoriaPendente.Add((entry, LogAuditoria.OperacaoLog.UPDATE,
                            SerializarValores(entry.OriginalValues), SerializarValores(entry.CurrentValues)));
                        break;
                    case EntityState.Deleted:
                        auditoriaPendente.Add((entry, LogAuditoria.OperacaoLog.DELETE,
                            SerializarValores(entry.OriginalValues), null));
                        break;
                }
            }

            // Usar transação interna para garantir atomicidade entre dados e auditoria.
            // Se já houver uma transação explícita (IUnitOfWork.BeginTransaction), o BeginTransaction
            // do provider cria uma savepoint; caso contrário, cria uma transação nova.
            bool usaTransacaoInterna = _transacaoAtual == null;
            IDbContextTransaction? transacaoInterna = null;

            try
            {
                if (usaTransacaoInterna)
                    transacaoInterna = await Database.BeginTransactionAsync(cancellationToken);

                int linhasAfetadas = await base.SaveChangesAsync(cancellationToken);

                // Após salvar, as entidades ADDED já possuem o Id gerado pelo banco.
                if (auditoriaPendente.Count > 0)
                {
                    // Membro autenticado que fez a alteração (null em cadastro/login)
                    int? idMembro = _usuarioAtual?.MembroId;

                    foreach (var (entry, operacao, dadosAntigos, dadosNovos) in auditoriaPendente)
                    {
                        string nomeTabela = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name;
                        int idRegistro = ((BaseEntity)entry.Entity).Id;

                        LogsAuditoria.Add(new LogAuditoria(
                            nomeTabela, idRegistro, operacao, dadosAntigos, dadosNovos, idMembro));
                    }

                    await base.SaveChangesAsync(cancellationToken);
                }

                if (usaTransacaoInterna && transacaoInterna != null)
                    await transacaoInterna.CommitAsync(cancellationToken);

                return linhasAfetadas;
            }
            catch
            {
                if (usaTransacaoInterna && transacaoInterna != null)
                    await transacaoInterna.RollbackAsync(cancellationToken);
                throw;
            }
            finally
            {
                if (transacaoInterna != null)
                    await transacaoInterna.DisposeAsync();
            }
        }

        private static string? SerializarValores(PropertyValues valores)
        {
            var dicionario = new Dictionary<string, object?>();
            foreach (var propriedade in valores.Properties)
            {
                if (CamposSensiveis.Contains(propriedade.Name))
                    continue;
                dicionario[propriedade.Name] = valores[propriedade];
            }

            return JsonSerializer.Serialize(dicionario);
        }

        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ============================================================
            // RELACIONAMENTOS EXPLÍCITOS (GAP 2)
            // ============================================================

            // Conta (individual) pertence a exatamente um Membro (dono).
            // FK shadow "MembroId": as entidades de domínio não expõem FK.
            modelBuilder.Entity<Categoria>()
            .Property(c => c.Necessario)
            .HasDefaultValue(true);

            // Conta (individual) pertence a exatamente um Membro (dono).
            // A propriedade MembroId (ex-FK shadow "IdMembro") agora é exposta
            // pela própria entidade e mapeia para a coluna "MembroId".
            modelBuilder.Entity<Conta>()
                .HasOne<Membro>()
                .WithMany(m => m.ContasIndividuais)
                .HasForeignKey(c => c.MembroId)
                .IsRequired()
                .HasConstraintName("FK_Conta_Membro")
                .OnDelete(DeleteBehavior.Restrict);

            // Membro -> ContaPrincipal (0..1). Aponta para a conta individual principal do membro.
            // Cria uma referência circular (Membro -> Conta -> Membro), por isso o OnDelete definido
            // para não criar ciclo/requisição de cascade no banco. A integridade é garantida em
            // application layer (ExcluirConta bloqueia a exclusão da conta principal).
            modelBuilder.Entity<Membro>()
                .HasOne<Conta>()
                .WithOne()
                .HasForeignKey<Membro>(m => m.ContaPrincipalId)
                .IsRequired(false)
                .HasConstraintName("FK_Membros_Contas_ContaPrincipalId")
                .OnDelete(DeleteBehavior.SetNull);

            // ContaConjunta <-> Membro (muitos-para-muitos) com tabela de junção.
            modelBuilder.Entity<ContaConjunta>()
                .HasMany(cc => cc.Membros)
                .WithMany(m => m.ContasConjuntas)
                .UsingEntity<Dictionary<string, object>>(
                    "ContaConjuntaMembro",
                    j => j
                        .HasOne<Membro>()
                        .WithMany()
                        .HasForeignKey("MembroId")
                        .HasConstraintName("FK_ContaConjuntaMembro_Membro"),
                    j => j
                        .HasOne<ContaConjunta>()
                        .WithMany()
                        .HasForeignKey("ContaConjuntaId")
                        .HasConstraintName("FK_ContaConjuntaMembro_ContaConjunta"),
                    j => j
                        .ToTable("ContaConjuntaMembro"));

            // ContaConjunta -> Membro criador (FK para quem criou a conta).
            modelBuilder.Entity<ContaConjunta>()
                .HasOne<Membro>()
                .WithMany()
                .HasForeignKey(cc => cc.CriadorId)
                .IsRequired()
                .HasConstraintName("FK_ContasConjuntas_Membros_CriadorId")
                .OnDelete(DeleteBehavior.Restrict);

            // Transacao -> Conta (individual), FK shadow opcional.
            modelBuilder.Entity<Transacao>()
                .HasOne(t => t.Conta)
                .WithMany(c => c.Transacoes)
                .HasForeignKey("ContaId")
                .IsRequired(false)
                .HasConstraintName("FK_Transacao_Conta")
                .OnDelete(DeleteBehavior.Restrict);

            // Transacao -> ContaConjunta, FK shadow opcional.
            modelBuilder.Entity<Transacao>()
                .HasOne(t => t.ContaConjunta)
                .WithMany(cc => cc.Transacoes)
                .HasForeignKey("ContaConjuntaId")
                .IsRequired(false)
                .HasConstraintName("FK_Transacao_ContaConjunta")
                .OnDelete(DeleteBehavior.Restrict);

            // Transacao -> Membro (autor da transação), FK shadow "MembroId".
            modelBuilder.Entity<Transacao>()
                .HasOne(t => t.Membro)
                .WithMany()
                .HasForeignKey("MembroId")
                .IsRequired()
                .HasConstraintName("FK_Transacoes_Membros_MembroId")
                .OnDelete(DeleteBehavior.Restrict);

            // Transacao -> Categoria (OPCIONAL), FK shadow "CategoriaId".
            modelBuilder.Entity<Transacao>()
                .HasOne(t => t.Categoria)
                .WithMany()
                .HasForeignKey("CategoriaId")
                .IsRequired(false)
                .HasConstraintName("FK_Transacoes_Categorias_CategoriaId")
                .OnDelete(DeleteBehavior.Restrict);

            // Categoria <-> SubCategoria (muitos-para-muitos) com tabela de junção.
            // A SubCategoria é um rótulo independente: pode ser vinculada a várias categorias.
            modelBuilder.Entity<Categoria>()
                .HasMany(c => c.SubCategorias)
                .WithMany(s => s.Categorias)
                .UsingEntity<Dictionary<string, object>>(
                    "CategoriaSubCategoria",
                    j => j
                        .HasOne<SubCategoria>()
                        .WithMany()
                        .HasForeignKey("SubCategoriaId")
                        .HasConstraintName("FK_CategoriaSubCategoria_SubCategorias_SubCategoriaId"),
                    j => j
                        .HasOne<Categoria>()
                        .WithMany()
                        .HasForeignKey("CategoriaId")
                        .HasConstraintName("FK_CategoriaSubCategoria_Categorias_CategoriaId"),
                    j => j.ToTable("CategoriaSubCategoria"));

            // Notificacao -> Membro, FK shadow "MembroId".
            modelBuilder.Entity<Notificacao>()
                .HasOne(n => n.Membro)
                .WithMany()
                .HasForeignKey("MembroId")
                .IsRequired()
                .HasConstraintName("FK_Notificacoes_Membros_MembroId")
                .OnDelete(DeleteBehavior.Restrict);

            // Notificacao não é ISoftDeletable, mas Membro (extremidade obrigatória da relação)
            // tem filtro global de soft-delete. Sem filtro correspondente aqui, o EF Core emite
            // o warning de model validation 10622. Este filtro espelha o soft-delete de Membro:
            // notificações de membros inativos não são retornadas.
            modelBuilder.Entity<Notificacao>()
                .HasQueryFilter(n => n.Membro.Ativo);

            // Meta -> Membro (opcional). MembroId é uma propriedade real na entidade,
            // mas não há navegação — usamos HasForeignKey por expressão.
            modelBuilder.Entity<Meta>()
                .HasOne<Membro>()
                .WithMany()
                .HasForeignKey(m => m.MembroId)
                .IsRequired(false)
                .HasConstraintName("FK_Metas_Membros_MembroId")
                .OnDelete(DeleteBehavior.SetNull);

            // LogAuditoria -> Membro (opcional). IdMembro é propriedade real sem navegação.
            modelBuilder.Entity<LogAuditoria>()
                .HasOne<Membro>()
                .WithMany()
                .HasForeignKey(l => l.IdMembro)
                .IsRequired(false)
                .HasConstraintName("FK_LogsAuditoria_Membros_IdMembro")
                .OnDelete(DeleteBehavior.SetNull);

            // ============================================================
            // META x TRANSACAO (N:N via tabela de junção MetaTransacao)
            // ============================================================
            // MetaTransacao -> Meta (FK para a meta à qual a transação pertence)
            modelBuilder.Entity<MetaTransacao>()
                .HasOne<Meta>()
                .WithMany(m => m.MetaTransacoes)
                .HasForeignKey(mt => mt.MetaId)
                .IsRequired()
                .HasConstraintName("FK_MetaTransacao_Meta")
                .OnDelete(DeleteBehavior.Cascade);

            // MetaTransacao -> Transacao (FK para a transação vinculada)
            modelBuilder.Entity<MetaTransacao>()
                .HasOne<Transacao>()
                .WithMany()
                .HasForeignKey(mt => mt.TransacaoId)
                .IsRequired()
                .HasConstraintName("FK_MetaTransacao_Transacao")
                .OnDelete(DeleteBehavior.Cascade);

            // Índice único para impedir vinculação duplicada da mesma transação à mesma meta
            modelBuilder.Entity<MetaTransacao>()
                .HasIndex(mt => new { mt.MetaId, mt.TransacaoId })
                .IsUnique()
                .HasDatabaseName("IX_MetaTransacao_MetaId_TransacaoId_Unico");

            // ============================================================
            // CATEGORIA: pertence a Conta OU ContaConjunta (XOR)
            // ============================================================
            modelBuilder.Entity<Categoria>()
                .HasOne(c => c.Conta)
                .WithMany(co => co.Categorias)
                .HasForeignKey(c => c.ContaId)
                .IsRequired(false)
                .HasConstraintName("FK_Categorias_Conta")
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Categoria>()
                .HasOne(c => c.ContaConjunta)
                .WithMany(cc => cc.Categorias)
                .HasForeignKey(c => c.ContaConjuntaId)
                .IsRequired(false)
                .HasConstraintName("FK_Categorias_ContaConjunta")
                .OnDelete(DeleteBehavior.Restrict);

            // ============================================================
            // SUBCATEGORIA: pertence a Conta OU ContaConjunta (XOR)
            // ============================================================
            modelBuilder.Entity<SubCategoria>()
                .HasOne(s => s.Conta)
                .WithMany(co => co.SubCategorias)
                .HasForeignKey(s => s.ContaId)
                .IsRequired(false)
                .HasConstraintName("FK_SubCategorias_Conta")
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SubCategoria>()
                .HasOne(s => s.ContaConjunta)
                .WithMany(cc => cc.SubCategorias)
                .HasForeignKey(s => s.ContaConjuntaId)
                .IsRequired(false)
                .HasConstraintName("FK_SubCategorias_ContaConjunta")
                .OnDelete(DeleteBehavior.Restrict);

            // ============================================================
            // REGRA DE NEGÓCIO NO BANCO: CHECK constraints XOR
            // ============================================================
            // Transacao: exatamente uma Conta OU ContaConjunta
            modelBuilder.Entity<Transacao>()
                .ToTable(t => t.HasCheckConstraint(
                    "CHK_Transacao_UmOuOutroTipoConta",
                    "(\"ContaId\" IS NOT NULL AND \"ContaConjuntaId\" IS NULL) OR " +
                    "(\"ContaId\" IS NULL AND \"ContaConjuntaId\" IS NOT NULL)"));

            // Categoria: validação XOR aplicada no domínio (DefinirConta/DefinirContaConjunta).
            // CHECK constraint no banco será adicionada futuramente quando dados existentes estiverem limpos.

            // SubCategoria: validação XOR aplicada no domínio (DefinirConta/DefinirContaConjunta).
            // CHECK constraint no banco será adicionada futuramente quando dados existentes estiverem limpos.

            // ============================================================
            // ÍNDICES
            // ============================================================
            modelBuilder.Entity<Conta>()
                .HasIndex(c => c.MembroId)
                .HasDatabaseName("IX_Contas_MembroId");

            modelBuilder.Entity<Transacao>()
                .HasIndex("ContaId")
                .HasDatabaseName("IX_Transacoes_ContaId");

            modelBuilder.Entity<Transacao>()
                .HasIndex("ContaConjuntaId")
                .HasDatabaseName("IX_Transacoes_ContaConjuntaId");

            modelBuilder.Entity<Transacao>()
                .HasIndex("MembroId")
                .HasDatabaseName("IX_Transacoes_MembroId");

            modelBuilder.Entity<Transacao>()
                .HasIndex(t => t.DataTransacao)
                .HasDatabaseName("IX_Transacoes_DataTransacao");

            modelBuilder.Entity<Transacao>()
                .HasIndex("CategoriaId")
                .HasDatabaseName("IX_Transacoes_CategoriaId");

            modelBuilder.Entity<Transacao>()
                .HasIndex("SubCategoriaId")
                .HasDatabaseName("IX_Transacoes_SubCategoriaId");

            modelBuilder.Entity<Notificacao>()
                .HasIndex("MembroId")
                .HasDatabaseName("IX_Notificacoes_MembroId");

            // Anti-repetição: um aviso por chave para cada membro (lidas ou não)
            modelBuilder.Entity<Notificacao>()
                .Property(n => n.Chave)
                .HasMaxLength(Notificacao.TamanhoMaximoChave);

            modelBuilder.Entity<Notificacao>()
                .HasIndex("MembroId", nameof(Notificacao.Chave))
                .IsUnique()
                .HasFilter("\"Chave\" IS NOT NULL")
                .HasDatabaseName("IX_Notificacoes_Membro_Chave_Unico");

            // PreferenciaNotificacao -> Membro (uma linha por membro)
            modelBuilder.Entity<PreferenciaNotificacao>()
                .HasOne<Membro>()
                .WithOne()
                .HasForeignKey<PreferenciaNotificacao>(p => p.MembroId)
                .IsRequired()
                .HasConstraintName("FK_PreferenciasNotificacao_Membros_MembroId")
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PreferenciaNotificacao>()
                .HasIndex(p => p.MembroId)
                .IsUnique()
                .HasDatabaseName("IX_PreferenciasNotificacao_MembroId_Unico");

            // PreferenciaUso -> Membro (uma linha por membro)
            modelBuilder.Entity<PreferenciaUso>()
                .HasOne<Membro>()
                .WithOne()
                .HasForeignKey<PreferenciaUso>(p => p.MembroId)
                .IsRequired()
                .HasConstraintName("FK_PreferenciasUso_Membros_MembroId")
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PreferenciaUso>()
                .HasIndex(p => p.MembroId)
                .IsUnique()
                .HasDatabaseName("IX_PreferenciasUso_MembroId_Unico");

            modelBuilder.Entity<PreferenciaUso>()
                .Property(p => p.ContaPadrao)
                .HasMaxLength(30);

            // CodigoRecuperacaoSenha -> Membro ("Esqueceu a senha": vários códigos por membro)
            modelBuilder.Entity<CodigoRecuperacaoSenha>()
                .HasOne<Membro>()
                .WithMany()
                .HasForeignKey(c => c.MembroId)
                .IsRequired()
                .HasConstraintName("FK_CodigosRecuperacaoSenha_Membros_MembroId")
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CodigoRecuperacaoSenha>()
                .HasIndex(c => c.MembroId)
                .HasDatabaseName("IX_CodigosRecuperacaoSenha_MembroId");

            modelBuilder.Entity<Meta>()
                .HasIndex(m => m.MembroId)
                .HasDatabaseName("IX_Metas_MembroId");

            modelBuilder.Entity<LogAuditoria>()
                .HasIndex(l => l.IdMembro)
                .HasDatabaseName("IX_LogsAuditoria_IdMembro");

            // NomeDeUsuario: único (usado como login)
            modelBuilder.Entity<Membro>()
                .HasIndex(m => m.NomeDeUsuario)
                .IsUnique()
                .HasDatabaseName("IX_Membros_NomeDeUsuario_Unico");

            // Email: único SEM diferenciar maiúsculas/minúsculas, igual à aplicação
            // (EfMembroRepository.ObterPorEmailAsync compara em minúsculas). O EF Core não
            // modela índice por expressão, então o índice único em lower("Email")
            // ("IX_Membros_Email_Unico") é criado por SQL na migração EmailUnicoSemDiferenciarMaiusculas.

            // Categoria: índices nas FKs de pertencimento
            modelBuilder.Entity<Categoria>()
                .HasIndex(c => c.ContaId)
                .HasDatabaseName("IX_Categorias_ContaId");

            modelBuilder.Entity<Categoria>()
                .HasIndex(c => c.ContaConjuntaId)
                .HasDatabaseName("IX_Categorias_ContaConjuntaId");

            // SubCategoria: índices nas FKs de pertencimento
            modelBuilder.Entity<SubCategoria>()
                .HasIndex(s => s.ContaId)
                .HasDatabaseName("IX_SubCategorias_ContaId");

            modelBuilder.Entity<SubCategoria>()
                .HasIndex(s => s.ContaConjuntaId)
                .HasDatabaseName("IX_SubCategorias_ContaConjuntaId");

            // ============================================================
            // CONVITE PARA CONTA CONJUNTA
            // ============================================================
            // ConviteContaConjunta -> ContaConjunta (conta para a qual o membro foi convidado)
            modelBuilder.Entity<ConviteContaConjunta>()
                .HasOne(c => c.ContaConjunta)
                .WithMany()
                .HasForeignKey(c => c.ContaConjuntaId)
                .IsRequired()
                .HasConstraintName("FK_Convites_ContasConjuntas_ContaConjuntaId")
                .OnDelete(DeleteBehavior.Restrict);

            // ConviteContaConjunta -> Membro convidado
            modelBuilder.Entity<ConviteContaConjunta>()
                .HasOne(c => c.Convidado)
                .WithMany()
                .HasForeignKey(c => c.ConvidadoId)
                .IsRequired()
                .HasConstraintName("FK_Convites_Membros_ConvidadoId")
                .OnDelete(DeleteBehavior.Restrict);

            // ConviteContaConjunta -> Membro que convidou (criador da conta)
            modelBuilder.Entity<ConviteContaConjunta>()
                .HasOne(c => c.ConvidadoPor)
                .WithMany()
                .HasForeignKey(c => c.ConvidadoPorId)
                .IsRequired()
                .HasConstraintName("FK_Convites_Membros_ConvidadoPorId")
                .OnDelete(DeleteBehavior.Restrict);

            // Espelha o soft delete das extremidades (como em Notificacao): convites de
            // contas conjuntas excluídas ou de membros inativos não são retornados.
            modelBuilder.Entity<ConviteContaConjunta>()
                .HasQueryFilter(c => c.ContaConjunta.Ativo && c.Convidado.Ativo && c.ConvidadoPor.Ativo);

            // Regra no banco: no máximo um convite PENDENTE por membro em cada conta conjunta
            modelBuilder.Entity<ConviteContaConjunta>()
                .HasIndex(c => new { c.ContaConjuntaId, c.ConvidadoId })
                .IsUnique()
                .HasFilter("\"Status\" = 0")
                .HasDatabaseName("IX_Convites_ContaConjunta_Convidado_Pendente");

            modelBuilder.Entity<ConviteContaConjunta>()
                .HasIndex(c => c.ConvidadoId)
                .HasDatabaseName("IX_Convites_ConvidadoId");

            // ============================================================
            // RECORRÊNCIA (série que gera transações mensais/semanais)
            // ============================================================
            // Recorrencia -> Membro (autor da série = autor das ocorrências), FK shadow "MembroId".
            modelBuilder.Entity<Recorrencia>()
                .HasOne(r => r.Membro)
                .WithMany()
                .HasForeignKey("MembroId")
                .IsRequired()
                .HasConstraintName("FK_Recorrencias_Membros_MembroId")
                .OnDelete(DeleteBehavior.Restrict);

            // Recorrencia -> Conta OU ContaConjunta (XOR, como a transação), FKs shadow opcionais.
            modelBuilder.Entity<Recorrencia>()
                .HasOne(r => r.Conta)
                .WithMany()
                .HasForeignKey("ContaId")
                .IsRequired(false)
                .HasConstraintName("FK_Recorrencias_Contas_ContaId")
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Recorrencia>()
                .HasOne(r => r.ContaConjunta)
                .WithMany()
                .HasForeignKey("ContaConjuntaId")
                .IsRequired(false)
                .HasConstraintName("FK_Recorrencias_ContasConjuntas_ContaConjuntaId")
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Recorrencia>()
                .HasOne(r => r.Categoria)
                .WithMany()
                .HasForeignKey("CategoriaId")
                .IsRequired(false)
                .HasConstraintName("FK_Recorrencias_Categorias_CategoriaId")
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Recorrencia>()
                .HasOne(r => r.SubCategoria)
                .WithMany()
                .HasForeignKey("SubCategoriaId")
                .IsRequired(false)
                .HasConstraintName("FK_Recorrencias_SubCategorias_SubCategoriaId")
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Recorrencia>()
                .ToTable(t => t.HasCheckConstraint(
                    "CHK_Recorrencia_UmOuOutroTipoConta",
                    "(\"ContaId\" IS NOT NULL AND \"ContaConjuntaId\" IS NULL) OR " +
                    "(\"ContaId\" IS NULL AND \"ContaConjuntaId\" IS NOT NULL)"));

            // Espelha o soft delete do autor (como em Notificacao): séries de membros inativos não são retornadas.
            modelBuilder.Entity<Recorrencia>()
                .HasQueryFilter(r => r.Membro.Ativo);

            modelBuilder.Entity<Recorrencia>()
                .HasIndex("MembroId")
                .HasDatabaseName("IX_Recorrencias_MembroId");

            // Transacao -> Recorrencia (série que gerou a ocorrência), FK shadow opcional.
            modelBuilder.Entity<Transacao>()
                .HasOne(t => t.Recorrencia)
                .WithMany()
                .HasForeignKey("RecorrenciaId")
                .IsRequired(false)
                .HasConstraintName("FK_Transacoes_Recorrencias_RecorrenciaId")
                .OnDelete(DeleteBehavior.Restrict);

            // Anti-repetição: uma ocorrência por série e data (vale também para as excluídas,
            // assim "excluir só esta" não recria a mesma data)
            modelBuilder.Entity<Transacao>()
                .HasIndex("RecorrenciaId", nameof(Transacao.DataTransacao))
                .IsUnique()
                .HasFilter("\"RecorrenciaId\" IS NOT NULL")
                .HasDatabaseName("IX_Transacoes_Recorrencia_Data_Unico");

            // ============================================================
            // SOFT DELETE (GAP 5): filtro global em todas as entidades ISoftDeletable
            // ============================================================
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
                {
                    var parametro = Expression.Parameter(entityType.ClrType, "e");
                    var propriedadeAtivo = Expression.Property(parametro, nameof(ISoftDeletable.Ativo));
                    var corpo = Expression.Equal(propriedadeAtivo, Expression.Constant(true));
                    var filtro = Expression.Lambda(corpo, parametro);

                    entityType.SetQueryFilter(filtro);
                }
            }

            base.OnModelCreating(modelBuilder);
        }
    }
}
