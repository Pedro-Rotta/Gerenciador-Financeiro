using GerenciadorContaFamiliar.Domain.Shared;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;

namespace GerenciadorContaFamiliar.Domain;

/// <summary>
/// Rótulo reutilizável (ex.: "Combustível", "Peças").
/// NÃO pertence a uma categoria fixa — pode ser vinculada a várias
/// categorias por meio do relacionamento muitos-para-muitos.
/// Pertence a uma Conta ou ContaConjunta (XOR).
/// </summary>
public class SubCategoria : BaseEntity, ISoftDeletable
{
    public string Nome { get; private set; }
    public bool Ativo { get; private set; } = true;

    // ---- Pertencimento: XOR — ou Conta ou ContaConjunta (nunca ambos) ----
    public int? ContaId { get; private set; }
    public int? ContaConjuntaId { get; private set; }
    public Conta? Conta { get; private set; }
    public ContaConjunta? ContaConjunta { get; private set; }

    // N:N — subcategoria vinculada a várias categorias (junção CategoriaSubCategoria).
    private readonly List<Categoria> _categorias = new();
    public IReadOnlyCollection<Categoria> Categorias => _categorias.AsReadOnly();

    // Construtor privado usado pelo EF Core ao materializar entidades do banco.
#pragma warning disable CS8618
    private SubCategoria() { }
#pragma warning restore CS8618

    public SubCategoria(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("O nome é obrigatório.");
        Nome = nome;
    }

    /// <summary>
    /// Vincula esta subcategoria a uma Conta individual.
    /// Não pode ter ContaConjunta vinculada ao mesmo tempo (XOR).
    /// </summary>
    public void DefinirConta(int contaId)
    {
        if (ContaConjuntaId.HasValue)
            throw new InvalidOperationException(
                "Esta subcategoria já está vinculada a uma ContaConjunta. " +
                "Desvincule antes de vincular a uma Conta.");
        ContaId = contaId;
        ContaConjuntaId = null;
    }

    /// <summary>
    /// Vincula esta subcategoria a uma ContaConjunta.
    /// Não pode ter Conta vinculada ao mesmo tempo (XOR).
    /// </summary>
    public void DefinirContaConjunta(int contaConjuntaId)
    {
        if (ContaId.HasValue)
            throw new InvalidOperationException(
                "Esta subcategoria já está vinculada a uma Conta. " +
                "Desvincule antes de vincular a uma ContaConjunta.");
        ContaId = null;
        ContaConjuntaId = contaConjuntaId;
    }

    public void VincularCategoria(Categoria categoria)
    {
        if (categoria is null)
            throw new ArgumentException("A categoria é obrigatória.");
        if (!_categorias.Contains(categoria))
            _categorias.Add(categoria);
    }

    public void DesvincularCategoria(Categoria categoria)
    {
        _categorias.Remove(categoria);
    }

    public void AtualizarNome(string novoNome)
    {
        if (string.IsNullOrWhiteSpace(novoNome))
            throw new ArgumentException("O nome não pode ser nulo.");
        Nome = novoNome;
    }

    public void Ativar() => Ativo = true;
    public void Inativar() => Ativo = false;
    public override string ToString() => $"[{Id}] {Nome}";
}
