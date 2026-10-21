using GerenciadorContaFamiliar.Domain.Shared;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;

namespace GerenciadorContaFamiliar.Domain;

public class Categoria : BaseEntity, ISoftDeletable
{
    public enum TipoCategoria { ENTRADA, SAIDA}

    public string Nome { get; private set; }
    public TipoCategoria Tipo { get; private set; }
    public bool Necessario { get; private set; }
    public string? Icone { get; private set; }
    public bool Ativo { get; private set; } = true;

    // ---- Pertencimento: XOR — ou Conta ou ContaConjunta (nunca ambos) ----
    public int? ContaId { get; private set; }
    public int? ContaConjuntaId { get; private set; }
    public Conta? Conta { get; private set; }
    public ContaConjunta? ContaConjunta { get; private set; }

    // N:N — subcategorias vinculadas a esta categoria (junção CategoriaSubCategoria).
    private readonly List<SubCategoria> _subCategorias = new();
    public IReadOnlyCollection<SubCategoria> SubCategorias => _subCategorias.AsReadOnly();

    // Construtor privado usado pelo EF Core ao materializar entidades do banco.
#pragma warning disable CS8618
    private Categoria() { }
#pragma warning restore CS8618

    public Categoria(string nome, TipoCategoria tipo, bool necessario = true, string? icone = null)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("O nome é obrigatório.");
        if (!Enum.IsDefined(typeof(TipoCategoria), tipo))
            throw new ArgumentException("O tipo é invalido");

        Nome = nome;
        Tipo = tipo;
        Necessario = necessario;
        Icone = icone;
    }

    /// <summary>
    /// Vincula esta categoria a uma Conta individual.
    /// Não pode ter ContaConjunta vinculada ao mesmo tempo (XOR).
    /// </summary>
    public void DefinirConta(int contaId)
    {
        if (ContaConjuntaId.HasValue)
            throw new InvalidOperationException(
                "Esta categoria já está vinculada a uma ContaConjunta. " +
                "Desvincule antes de vincular a uma Conta.");
        ContaId = contaId;
        ContaConjuntaId = null;
    }

    /// <summary>
    /// Vincula esta categoria a uma ContaConjunta.
    /// Não pode ter Conta vinculada ao mesmo tempo (XOR).
    /// </summary>
    public void DefinirContaConjunta(int contaConjuntaId)
    {
        if (ContaId.HasValue)
            throw new InvalidOperationException(
                "Esta categoria já está vinculada a uma Conta. " +
                "Desvincule antes de vincular a uma ContaConjunta.");
        ContaId = null;
        ContaConjuntaId = contaConjuntaId;
    }

    public void AtualizarNome(string novoNome)
    {
        if (string.IsNullOrWhiteSpace(novoNome))
            throw new ArgumentException("O nome não pode ser nulo.");
        Nome = novoNome;
    }
    public void AtualizarTipo(TipoCategoria novoTipo)
    {
        if (!Enum.IsDefined(typeof(TipoCategoria), novoTipo))
            throw new ArgumentException("O tipo é invalido");
        Tipo = novoTipo;
    }
    public void AtualizarNecessario(bool novoNecessario)
    {
        Necessario = novoNecessario;
    }
    public void AtualizarIcone(string? novoIcone)
    {
        Icone = novoIcone;
    }

    public void Ativar() => Ativo = true;
    public void Inativar() => Ativo = false;

    public void VincularSubCategoria(SubCategoria subCategoria)
    {
        if (subCategoria is null)
            throw new ArgumentException("A subcategoria é obrigatória.");
        if (!_subCategorias.Contains(subCategoria))
            _subCategorias.Add(subCategoria);
    }

    public void DesvincularSubCategoria(SubCategoria subCategoria)
    {
        _subCategorias.Remove(subCategoria);
    }

    public override string ToString() => $"[{Id}] {Nome}";
}
