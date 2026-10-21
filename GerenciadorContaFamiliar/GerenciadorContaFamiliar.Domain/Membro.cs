using GerenciadorContaFamiliar.Domain.Shared;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
namespace GerenciadorContaFamiliar.Domain;

public class Membro : BaseEntity, ISoftDeletable
{
    public string Nome { get; private set; }
    public string NomeDeUsuario { get; private set; }
    public string Email { get; private set; }
    public string? Telefone { get; private set; }
    public DateOnly? DataNascimento { get; private set; }
    public string? SenhaHash { get; private set; }

    /// <summary>Sobe a cada troca de senha; token com versão diferente é recusado.</summary>
    public int VersaoSessao { get; private set; }
    public bool Ativo { get; private set; }
    public DateTime CriadoEm { get; private set; }
    /// <summary>
    /// Id da conta principal do membro. A conta principal é criada automaticamente
    /// logo após a criação do membro e aponta para a conta individual marcada como principal.
    /// Null enquanto o membro ainda não possui conta principal.
    /// </summary>
    public int? ContaPrincipalId { get; private set; }
    public List<ContaConjunta> ContasConjuntas { get; private set; } = new();
    public List<Conta> ContasIndividuais { get; private set; } = new();

    // Construtor privado usado pelo EF Core ao materializar entidades do banco.
#pragma warning disable CS8618
    private Membro() { }
#pragma warning restore CS8618

    public Membro(string nome, string nomeDeUsuario, string email, string? telefone, DateOnly? dataNascimento = null)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("O nome é obrigatório.");
        if (string.IsNullOrWhiteSpace(nomeDeUsuario))
            throw new ArgumentException("O nome de usuário é obrigatório.");
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("O email é obrigatório.");

        Nome = nome;
        NomeDeUsuario = nomeDeUsuario;
        Email = email;
        Telefone = telefone;
        DataNascimento = dataNascimento;
        Ativo = true;
        CriadoEm = DateTime.UtcNow;
    }

    public void DefinirSenha(string senha, IPasswordHasher hasher)
    {
        if (string.IsNullOrWhiteSpace(senha))
            throw new ArgumentException("A senha é obrigatória.");
        SenhaHash = hasher.Hash(senha);
    }

    /// <summary>
    /// Troca a senha (alterar ou "esqueceu a senha") e derruba as sessões abertas: o JWT guarda
    /// a versão e a API recusa as antigas.
    /// </summary>
    public void TrocarSenha(string novaSenha, IPasswordHasher hasher)
    {
        if (string.IsNullOrWhiteSpace(novaSenha) || novaSenha.Length < 6 || novaSenha.Length > 100)
            throw new ArgumentException("A senha deve ter entre 6 e 100 caracteres");
        DefinirSenha(novaSenha, hasher);
        VersaoSessao++;
    }

    /// <summary>"Encerrar outras sessões": todos os tokens atuais deixam de valer.</summary>
    public void EncerrarSessoes() => VersaoSessao++;

    public bool VerificarSenha(string senha, IPasswordHasher hasher)
    {
        if (string.IsNullOrWhiteSpace(SenhaHash))
            return false;
        return hasher.Verify(senha, SenhaHash);
    }

    public void AdicionarContaConjunta(ContaConjunta conta)
    {
        if (!ContasConjuntas.Contains(conta))
            ContasConjuntas.Add(conta);
    }

    public void RemoverContaConjunta(ContaConjunta conta)
    {
        if (ContasConjuntas.Contains(conta))
            ContasConjuntas.Remove(conta);
    }

    public void AtualizarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("O nome não pode ser nulo.");
        
        Nome = nome;
    }

    /// <summary>
    /// Define a conta principal do membro. Usado após a criação da conta principal
    /// (que recebe seu Id apenas após o SaveChanges) ou ao trocar a conta principal.
    /// </summary>
    public void DefinirContaPrincipal(int contaId)
    {
        if (contaId <= 0)
            throw new ArgumentException("O identificador da conta principal é inválido.");
        ContaPrincipalId = contaId;
    }

    public void AtualizarNomeDeUsuario(string nomeDeUsuario)
    {
        if (string.IsNullOrWhiteSpace(nomeDeUsuario))
            throw new ArgumentException("O nome de usuário não pode ser nulo.");
        NomeDeUsuario = nomeDeUsuario;
    }

    public void AtualizarEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("O email não pode ser nulo.");
        Email = email;
    }

    public void AtualizarTelefone(string telefone)
    {
        Telefone = telefone;
    }

    public void AtualizarDataNascimento(DateOnly dataNascimento)
    {
        DataNascimento = dataNascimento;
    }

    public void Ativar() => Ativo = true;
    public void Inativar() => Ativo = false;

    public override string ToString() => $"[{Id}] {Nome}";
}