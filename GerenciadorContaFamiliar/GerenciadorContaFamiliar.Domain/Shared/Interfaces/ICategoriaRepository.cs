using GerenciadorContaFamiliar.Domain;

namespace GerenciadorContaFamiliar.Domain.Shared.Interfaces;

public interface ICategoriaRepository
{
    /// <summary>
    /// Obtém uma categoria pelo seu identificador único.
    /// <returns>Categoria encontrada ou null se não existir</returns>
    Task<Categoria?> ObterPorIdAsync(int id, CancellationToken ct = default);
    /// <summary>
    /// Obtém uma categoria pelo seu nome, restringindo à mesma conta (individual) ou
    /// conta conjunta do vínculo informado. Um (e apenas um) dos dois parâmetros deve ser preenchido (XOR).
    /// <returns>Categoria encontrada ou null se não existir</returns>
    Task<Categoria?> ObterPorNomeAsync(string nome, int? contaId, int? contaConjuntaId, CancellationToken ct = default);
    /// <summary>
    /// Obtém todas as categorias ativas.
    /// <returns>Lista de categorias</returns>
    Task<IReadOnlyList<Categoria>> ObterTodasAsync(CancellationToken ct = default);
    /// <summary>
    /// Obtém todas as categoria incluindo as subcategorias vinculadas a ela.
    /// <returns>Categoria com SubCategorias carregadas ou null se não existir</returns>
    Task<Categoria?> ObterComSubCategoriasAsync(int id, CancellationToken ct = default);
    /// <summary>
    /// Obtém todas as categorias de um determinado tipo (ENTRADA/SAIDA).
    /// <returns>Lista de categorias do tipo informado</returns>
    Task<IReadOnlyList<Categoria>> ObterPorTipoAsync(Categoria.TipoCategoria tipo, CancellationToken ct = default);
    /// <summary>
    /// Adiciona uma nova categoria ao repositório.
    /// As alterações serão persistidas no banco de dados quando o UnitOfWork for commitado.
    Task AdicionarAsync(Categoria categoria, CancellationToken ct = default);
    /// <summary>
    /// Atualiza uma categoria existente no repositório.
    /// As alterações serão persistidas no banco de dados quando o UnitOfWork for commitado.
    /// </summary>
    Task AtualizarAsync(Categoria categoria, CancellationToken ct = default);
    /// <summary>
    /// Remove uma categoria pelo seu identificador.
    /// A remoção será persistida no banco de dados quando o UnitOfWork for commitado.
    Task RemoverAsync(int id, CancellationToken ct = default);
    /// <summary>
    /// Obtém todas as categorias de uma determinada conta
    /// <returns>Lista de categorias do tipo informado</returns>
    Task<IReadOnlyList<Categoria>> ObterPorContaAsync(int contaId, CancellationToken ct = default);
    /// <summary>
    /// Obtém todas as categorias de uma determinada contaConjunta
    /// <returns>Lista de categorias do tipo informado</returns>
    Task<IReadOnlyList<Categoria>> ObterPorContaConjuntaAsync(int contaConjuntaId, CancellationToken ct = default);
}