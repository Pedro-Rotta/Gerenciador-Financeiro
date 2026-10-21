namespace GerenciadorContaFamiliar.Domain.Shared.Interfaces;

public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Inicia uma transação explícita no banco de dados.
    /// Agrupa múltiplos <see cref="SaveChangesAsync"/> em uma única transação atômica.
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirma a transação explícita iniciada por <see cref="BeginTransactionAsync"/>.
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Desfaz todas as alterações pendentes desde o início da transação explícita.
    /// </summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Descarta as alterações rastreadas e ainda não salvas (usado quando um passo falha,
    /// para que o próximo SaveChanges não grave o que sobrou dele).
    /// </summary>
    void DescartarAlteracoes();
}
