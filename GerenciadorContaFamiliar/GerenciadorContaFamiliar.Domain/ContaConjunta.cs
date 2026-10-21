using GerenciadorContaFamiliar.Domain.Shared;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
namespace GerenciadorContaFamiliar.Domain;

public class ContaConjunta : BaseEntity, ISoftDeletable, IContaTransacionavel
{
    public string Nome { get; private set; }
    public string? Descricao { get; private set; }
    public decimal Saldo { get; private set; } = 0m;
    public bool Ativo { get; private set; }
    /// <summary>
    /// ID do Membro que criou esta conta conjunta.
    /// O criador é automaticamente vinculado como membro da conta.
    /// </summary>
    public int CriadorId { get; private set; }
    public List<Transacao> Transacoes { get; private set; } = new();
    public List<Membro> Membros { get; private set; } = new();
    public List<Categoria> Categorias { get; private set; } = new();
    public List<SubCategoria> SubCategorias { get; private set; } = new();

    // Construtor privado usado pelo EF Core ao materializar entidades do banco.
#pragma warning disable CS8618
    private ContaConjunta() { }
#pragma warning restore CS8618

    public ContaConjunta(string nome, string? descricao, int criadorId)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("O nome é obrigatório.");
        if (criadorId <= 0)
            throw new ArgumentException("O criador é obrigatório.");

        Nome = nome;
        Descricao = descricao;
        CriadorId = criadorId;
        Ativo = true;
    }
public void EfetuarTransacao(Transacao transacao)
    {
        if(!Ativo)
            throw new InvalidOperationException("Conta inativa não pode receber transações");
        ConferirTransacao(transacao);

        // Só o que foi pago/recebido mexe no saldo; pendente e atrasada ficam no previsto.
        // Conta conjunta começa em 0, sem saldo inicial.
        if (transacao.Status == Transacao.StatusTransacao.CONCLUIDA)
            AplicarNoSaldo(transacao);
        AdicionarTransacao(transacao);
    }
    /// <summary>
    /// Desfaz o efeito de uma transação no saldo (usado ao excluir a transação): só a
    /// CONCLUÍDA tinha entrado no saldo. A transação continua vinculada à conta conjunta,
    /// pois a exclusão é lógica.
    /// </summary>
    public void EstornarTransacao(Transacao transacao)
    {
        if (transacao.Status == Transacao.StatusTransacao.CONCLUIDA)
            DesfazerNoSaldo(transacao);
        else
            ConferirTransacao(transacao);
    }
    public void AplicarNoSaldo(Transacao transacao)
    {
        ConferirTransacao(transacao);
        Saldo += transacao.EhEntrada() ? transacao.Valor : -transacao.Valor;
    }
    public void DesfazerNoSaldo(Transacao transacao)
    {
        ConferirTransacao(transacao);
        Saldo -= transacao.EhEntrada() ? transacao.Valor : -transacao.Valor;
    }
    private void ConferirTransacao(Transacao transacao)
    {
        if (transacao.ContaConjunta != this)
        {
            throw new InvalidOperationException(
                "Esta transação não pertence a esta conta conjunta. " +
                $"Esperado: ContaConjunta #{this.Id}, Recebido: ContaConjunta #{transacao.ContaConjunta?.Id ?? 0}"
            );
        }
    }
    public void AtualizarNome(string nome)
    {
        if(string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome nao pode ser nulo");
        Nome = nome;
    }
    public void AtualizarDescricao(string? descricao)
    {
        Descricao = descricao;
    }
    public void AdicionarMembro(Membro membro)
    {
        if (!Membros.Contains(membro))
            Membros.Add(membro);
    }
    public void RemoverMembro(Membro membro)
    {
        // O criador não sai da conta: para encerrá-la ele deve excluí-la
        if (membro.Id == CriadorId)
            throw new InvalidOperationException("O criador não pode sair nem ser removido da conta conjunta.");
        if (Membros.Contains(membro))
            Membros.Remove(membro);
    }
    public void AdicionarTransacao(Transacao transacao)
    {
        if (!Transacoes.Contains(transacao))
            Transacoes.Add(transacao);
    }
    public void RemoverTransacao(Transacao transacao)
    {
        if (Transacoes.Contains(transacao))
            Transacoes.Remove(transacao);
    }

    public void AdicionarCategoria(Categoria categoria)
    {
        if (!Categorias.Contains(categoria))
            Categorias.Add(categoria);
    }
    public void RemoverCategoria(Categoria categoria)
    {
        Categorias.Remove(categoria);
    }
    public void AdicionarSubCategoria(SubCategoria subCategoria)
    {
        if (!SubCategorias.Contains(subCategoria))
            SubCategorias.Add(subCategoria);
    }
    public void RemoverSubCategoria(SubCategoria subCategoria)
    {
        SubCategorias.Remove(subCategoria);
    }

    public void Ativar() => Ativo = true;
    public void Inativar() => Ativo = false;
    public override string ToString() => $"[{Id}] {Nome}";
}