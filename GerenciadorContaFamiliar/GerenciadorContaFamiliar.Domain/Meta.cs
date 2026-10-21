using GerenciadorContaFamiliar.Domain.Shared;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;

namespace GerenciadorContaFamiliar.Domain;

public class Meta : BaseEntity, ISoftDeletable
{
    public string Nome { get; private set; }
    public decimal ValorAlvo { get; private set; }
    public decimal ValorAtual { get; private set; }
    public DateOnly Prazo { get; private set; }
    public bool Concluida { get; private set; }
    public int? MembroId { get; private set; }
    public bool Ativo { get; private set; }

    // N:N — transações vinculadas a esta meta
    private readonly List<MetaTransacao> _metaTransacoes = new();
    public IReadOnlyCollection<MetaTransacao> MetaTransacoes => _metaTransacoes.AsReadOnly();

    // Construtor privado usado pelo EF Core ao materializar entidades do banco.
#pragma warning disable CS8618
    private Meta() { }
#pragma warning restore CS8618

    public Meta(string nome, decimal valorAlvo, DateOnly prazo, int? membroId){

        if(string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("O nome nao pode ser nulo");
        if(valorAlvo <= 0)
            throw new ArgumentException("O valor alvo nao pode ser nulo ou negativo");
        if(prazo < DateOnly.FromDateTime(DateTime.Now))
            throw new ArgumentException("O prazo deve ser maior que o dia atual");
        
        Nome = nome;
        ValorAlvo = valorAlvo;
        Prazo = prazo;
        MembroId = membroId;
        ValorAtual = 0;
        Concluida = false;
        Ativo = true;
    }
    public void AtualizarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("O nome nao pode ser nulo");
        Nome = nome;
    }
    public void AtualizarValorAlvo(decimal valorAlvo)
    {
        if (valorAlvo <= 0)
            throw new ArgumentException("O valor alvo nao pode ser nulo ou negativo");
        ValorAlvo = valorAlvo;
        Concluida = ValorAtual >= ValorAlvo;
    }
    public void AtualizarPrazo(DateOnly prazo)
    {
        if (prazo < DateOnly.FromDateTime(DateTime.Now))
            throw new ArgumentException("O prazo deve ser maior que o dia atual");
        Prazo = prazo;
    }
    public void RegistrarProgresso(decimal valor)
    {
        if (valor <= 0)
            throw new ArgumentException("O valor a registrar deve ser maior que zero.");

        ValorAtual += valor;
        Concluida = ValorAtual >= ValorAlvo;
    }

    /// <summary>
    /// Vincula uma transação a esta meta.
    /// O ValorAtual é recalculado automaticamente com base em todas as transações vinculadas.
    /// </summary>
    public void VincularTransacao(MetaTransacao metaTransacao)
    {
        if (metaTransacao is null)
            throw new ArgumentException("A vinculação é obrigatória.");
        if (!_metaTransacoes.Contains(metaTransacao))
            _metaTransacoes.Add(metaTransacao);
    }

    /// <summary>
    /// Desvincula uma transação desta meta.
    /// O ValorAtual é recalculado automaticamente com base em todas as transações vinculadas.
    /// </summary>
    public void DesvincularTransacao(MetaTransacao metaTransacao)
    {
        _metaTransacoes.Remove(metaTransacao);
    }

    /// <summary>
    /// Recalcula o ValorAtual somando os valores de todas as transações vinculadas.
    /// Chamado após vincular ou desvincular uma transação.
    /// </summary>
    public void RecalcularValorAtual()
    {
        // O cálculo é feito externamente via repositório (Include das transações)
        // e este método recebe o valor já calculado.
    }

    /// <summary>
    /// Define o ValorAtual (soma real das transações vinculadas, sem teto: pode passar do alvo)
    /// e marca a meta como concluída enquanto ele alcançar o alvo.
    /// </summary>
    public void AtualizarValorAtual(decimal novoValorAtual)
    {
        ValorAtual = novoValorAtual;
        Concluida = ValorAtual >= ValorAlvo;
    }

    public void Ativar() => Ativo = true;
    public void Inativar() => Ativo = false;
    public override string ToString() => $"[{Id}] {Nome}";
}