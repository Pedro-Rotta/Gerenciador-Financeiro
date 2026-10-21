using GerenciadorContaFamiliar.Domain;

namespace GerenciadorContaFamiliar.Domain.Shared.Interfaces;

public interface ILogAuditoriaRepository
{
    /// <summary>
    /// Obtém um log de auditoria pelo seu identificador único.
    /// </summary>
    /// <returns>Log encontrado ou null se não existir</returns>
    Task<LogAuditoria?> ObterPorIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Obtém todos os logs de auditoria de um determinado membro.
    /// </summary>
    /// <returns>Lista de logs do membro informado</returns>
    Task<IReadOnlyList<LogAuditoria>> ObterPorMembroAsync(int idMembro, CancellationToken ct = default);

    /// <summary>
    /// Adiciona um novo log de auditoria ao repositório.
    /// As alterações serão persistidas no banco de dados quando o UnitOfWork for commitado.
    /// </summary>
    Task AdicionarAsync(LogAuditoria log, CancellationToken ct = default);
}
