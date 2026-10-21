using GerenciadorContaFamiliar.Domain.Shared;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
namespace GerenciadorContaFamiliar.Domain;
public class Conta : BaseEntity, ISoftDeletable, IContaTransacionavel
{
    public string Nome { get; private set; }
    /// <summary>
    /// Saldo real: saldo inicial + entradas CONCLUÍDAS − saídas CONCLUÍDAS. Pendentes e
    /// atrasadas ficam fora (entram no saldo previsto, calculado na listagem).
    /// </summary>
    public decimal Saldo { get; private set; } = 0m;

    /// <summary>Valor informado uma única vez, antes da primeira transação (pode ser negativo).</summary>
    public decimal SaldoInicial { get; private set; } = 0m;

    /// <summary>Enquanto falso, a conta não aceita transações.</summary>
    public bool SaldoInicialDefinido { get; private set; }
    public int MembroId { get; private set; }
    public bool Ativo { get; private set; }
    /// <summary>
    /// Indica se esta é a conta principal do membro.
    /// Todo membro possui exatamente uma conta principal, mas pode ter várias outras contas.
    /// </summary>
    public bool ContaPrincipal { get; private set; }
    public bool EhContaPrincipal => ContaPrincipal;
    public List<Transacao> Transacoes { get; private set; } = new();
    public List<Categoria> Categorias { get; private set; } = new();
    public List<SubCategoria> SubCategorias { get; private set; } = new();

    // Construtor privado usado pelo EF Core ao materializar entidades do banco.
#pragma warning disable CS8618
    private Conta() { }
#pragma warning restore CS8618

    public Conta(string nome, int idMembro, bool contaPrincipal = false)
    {
        if(string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("O nome é obrigatório.");
        Nome = nome;
        MembroId = idMembro;
        Ativo = true;
        ContaPrincipal = contaPrincipal;
    }
    /// <summary>
    /// Informa o saldo inicial (uma vez só): soma ao saldo e libera os lançamentos.
    /// </summary>
    public void DefinirSaldoInicial(decimal valor)
    {
        if (SaldoInicialDefinido)
            throw new InvalidOperationException("O saldo inicial desta conta já foi informado.");
        SaldoInicial = valor;
        Saldo += valor;
        SaldoInicialDefinido = true;
    }
    public void EfetuarTransacao(Transacao transacao)
    {
        if(!Ativo)
            throw new InvalidOperationException("Conta inativa não pode receber transações");
        if (!SaldoInicialDefinido)
            throw new InvalidOperationException("Informe o saldo inicial da conta antes de lançar transações.");
        ConferirTransacao(transacao);

        // Só o que foi pago/recebido mexe no saldo; pendente e atrasada ficam no previsto
        if (transacao.Status == Transacao.StatusTransacao.CONCLUIDA)
            AplicarNoSaldo(transacao);
        AdicionarTransacao(transacao);
    }
    /// <summary>
    /// Desfaz o efeito de uma transação no saldo (usado ao excluir a transação): só a
    /// CONCLUÍDA tinha entrado no saldo. A transação continua vinculada à conta, pois a
    /// exclusão é lógica.
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
        if (transacao.Conta != this)
        {
            throw new InvalidOperationException(
                "Esta transação não pertence a esta conta. " +
                $"Esperado: Conta #{this.Id}, Recebido: Conta #{transacao.Conta?.Id ?? 0}"
            );
        }
    }
    public void AtualizarNome(string nome)
    {
        if(string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome nao pode ser nulo");
        Nome = nome;
    }
    public void MarcarComoPrincipal() => ContaPrincipal = true;
    public void DestituirDePrincipal() => ContaPrincipal = false;
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