using GerenciadorContaFamiliar.Domain;

namespace GerenciadorContaFamiliar.Application.UseCases.Categorias;

/// <summary>
/// Categorias base criadas automaticamente em toda conta nova (individual ou conjunta).
/// </summary>
public static class CategoriasPadrao
{
    public static IEnumerable<Categoria> Criar()
    {
        yield return new Categoria("Carro", Categoria.TipoCategoria.SAIDA, necessario: true);
        yield return new Categoria("Comida", Categoria.TipoCategoria.SAIDA, necessario: false);
        yield return new Categoria("Moradia", Categoria.TipoCategoria.SAIDA, necessario: true);
        yield return new Categoria("Lazer", Categoria.TipoCategoria.SAIDA, necessario: false);
        yield return new Categoria("Salário", Categoria.TipoCategoria.ENTRADA, necessario: false);
        yield return new Categoria("Extra", Categoria.TipoCategoria.ENTRADA, necessario: false);
    }
}
