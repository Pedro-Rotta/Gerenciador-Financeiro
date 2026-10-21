using GerenciadorContaFamiliar.Domain;

namespace GerenciadorContaFamiliar.Domain.Shared.Interfaces;

public interface ISubCategoriaRepository
{
    /// <summary>
    /// Obtém uma subcategoria pelo seu identificador único.
    /// </summary>
    /// <returns>Subcategoria encontrada ou null se não existir</returns>
    Task<SubCategoria?> ObterPorIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Obtém uma subcategoria pelo seu nome (para validação de duplicidade), restringindo à mesma
    /// conta (individual) ou conta conjunta informada. Um (e apenas um) dos dois parâmetros deve ser preenchido (XOR).
    /// </summary>
    Task<SubCategoria?> ObterPorNomeAsync(string nome, int? contaId, int? contaConjuntaId, CancellationToken ct = default);

    /// <summary>
    /// Obtém todas as subcategorias ativas (rótulos independentes).
    /// </summary>
    Task<IReadOnlyList<SubCategoria>> ObterTodasAsync(CancellationToken ct = default);

    /// <summary>
    /// Obtém as subcategorias vinculadas a uma determinada categoria.
    /// </summary>
    Task<IReadOnlyList<SubCategoria>> ObterVinculadasAsync(int categoriaId, CancellationToken ct = default);

    /// <summary>
    /// Obtém as subcategorias AINDA NÃO vinculadas a uma determinada categoria
    /// (para a tela "Vincular nova Subcategoria").
    /// </summary>
    Task<IReadOnlyList<SubCategoria>> ObterDisponiveisAsync(int categoriaId, CancellationToken ct = default);

    /// <summary>
    /// Adiciona uma nova subcategoria ao repositório.
    /// As alterações serão persistidas no banco de dados quando o UnitOfWork for commitado.
    /// </summary>
    Task AdicionarAsync(SubCategoria subCategoria, CancellationToken ct = default);

    /// <summary>
    /// Atualiza uma subcategoria existente no repositório.
    /// As alterações serão persistidas no banco de dados quando o UnitOfWork for commitado.
    /// </summary>
    Task AtualizarAsync(SubCategoria subCategoria, CancellationToken ct = default);

    /// <summary>
    /// Remove (soft delete) uma subcategoria pelo seu identificador.
    /// </summary>
    Task RemoverAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<SubCategoria>> ObterPorContaAsync(int contaId, CancellationToken ct = default);
    Task<IReadOnlyList<SubCategoria>> ObterPorContaConjuntaAsync(int contaConjuntaId, CancellationToken ct = default);
}
