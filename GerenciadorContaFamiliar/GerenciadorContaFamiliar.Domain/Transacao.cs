using GerenciadorContaFamiliar.Domain.Shared;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
namespace GerenciadorContaFamiliar.Domain;

public class Transacao : BaseEntity, ISoftDeletable
{
    public enum TipoTransacao { ENTRADA, SAIDA }
    public enum ClassificacaoTransacao { FIXA, VARIAVEL }
    public enum StatusTransacao { PENDENTE, CONCLUIDA, ATRASADA }
    public enum PeriodicidadeTransacao { MENSAL, SEMANAL }
    
    public decimal Valor { get; private set; }
    public string? Descricao { get; private set; }
    public DateOnly DataTransacao { get; private set; }
    public TipoTransacao Tipo { get; private set; }
    public ClassificacaoTransacao Classificacao { get; private set; }
    public bool Recorrente { get; private set; }
    public PeriodicidadeTransacao? Periodicidade { get; private set; }
    public StatusTransacao Status { get; private set; }
    public Membro Membro { get; private set; }
    public Categoria? Categoria { get; private set; }
    public SubCategoria? SubCategoria { get; private set; }
    public Conta? Conta { get; private set; }
    public ContaConjunta? ContaConjunta { get; private set; }
    /// <summary>Série recorrente que gerou esta transação (null = lançamento avulso).</summary>
    public Recorrencia? Recorrencia { get; private set; }
    /// <summary>Posição na série: 1, 2, 3...</summary>
    public int? NumeroOcorrencia { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public bool Ativo { get; private set; } = true;
    
#pragma warning disable CS8618
    private Transacao() { }
#pragma warning restore CS8618

    public Transacao(decimal valor, string? descricao, DateOnly dataTransacao, 
        TipoTransacao tipo, ClassificacaoTransacao classificacao, bool recorrente, 
        PeriodicidadeTransacao? periodicidade, 
        Membro membro, Categoria? categoria, SubCategoria? subCategoria,
        Conta? conta, ContaConjunta? contaConjunta, StatusTransacao status = StatusTransacao.CONCLUIDA)
    {
        if (valor <= 0)
            throw new ArgumentException("O valor deve ser maior que zero.");
        // Data futura só para PENDENTE (conta a pagar/receber); "hoje" é a data de Brasília
        if (dataTransacao > Relogio.HojeBrasilia() && status != StatusTransacao.PENDENTE)
            throw new ArgumentException("Só transações pendentes podem ter data no futuro.");
        if (membro == null)
            throw new ArgumentException("O membro é obrigatório.");
        if ((conta == null && contaConjunta == null) || (conta != null && contaConjunta != null))
            throw new ArgumentException("A transação deve estar vinculada a exatamente uma conta (individual ou conjunta).");
        // Valida o parâmetro recebido (a propriedade Periodicidade ainda não foi atribuída aqui)
        if (recorrente && !periodicidade.HasValue)
            throw new ArgumentException("A periodicidade precisa ter valor");
        if (!recorrente && periodicidade.HasValue)
            throw new ArgumentException("Transação não recorrente não pode ter periodicidade.");

        Valor = valor;
        Descricao = descricao;
        DataTransacao = dataTransacao;
        Tipo = tipo;
        Classificacao = classificacao;
        Recorrente = recorrente;
        Periodicidade = periodicidade;
        Status = AjustarAtraso(status, dataTransacao);
        Membro = membro;
        Categoria = categoria;
        SubCategoria = subCategoria;
        Conta = conta;
        ContaConjunta = contaConjunta;
        CriadoEm = DateTime.UtcNow;
    }
    public void AplicarEmConta() => ContaDaTransacao().EfetuarTransacao(this);
    public void EstornarDaConta() => ContaDaTransacao().EstornarTransacao(this);

    private IContaTransacionavel ContaDaTransacao()
    {
        if (Conta is not null && ContaConjunta is null)
            return Conta;
        if (Conta is null && ContaConjunta is not null)
            return ContaConjunta;
        throw new InvalidOperationException("Transação deve estar vinculada a uma Conta ou ContaConjunta.");
    }
    public bool EhEntrada()
    {
        return Tipo == TipoTransacao.ENTRADA;
    }

    public void AtualizarDescricao(string? descricao)
    {
        Descricao = descricao;
    }

    public void AtualizarStatus(StatusTransacao status)
    {
        if (!Enum.IsDefined(typeof(StatusTransacao), status))
            throw new ArgumentException("Status inválido.");
        // Mesma regra da criação: não dá para pagar (nem atrasar) o que ainda não chegou
        if (DataTransacao > Relogio.HojeBrasilia() && status != StatusTransacao.PENDENTE)
            throw new ArgumentException("Só transações pendentes podem ter data no futuro: a conta só pode ser paga a partir do dia dela.");

        var anterior = Status;
        Status = AjustarAtraso(status, DataTransacao);

        // Só a CONCLUÍDA está no saldo: pagar aplica o valor, voltar a pendente desfaz.
        // PENDENTE ↔ ATRASADA não toca na conta (nem precisa dela carregada).
        if (anterior != StatusTransacao.CONCLUIDA && Status == StatusTransacao.CONCLUIDA)
            ContaDaTransacao().AplicarNoSaldo(this);
        else if (anterior == StatusTransacao.CONCLUIDA && Status != StatusTransacao.CONCLUIDA)
            ContaDaTransacao().DesfazerNoSaldo(this);
    }

    /// <summary>Liga a transação à série que a gerou, como a ocorrência de número informado.</summary>
    public void VincularARecorrencia(Recorrencia recorrencia, int numeroOcorrencia)
    {
        if (numeroOcorrencia < 1)
            throw new ArgumentException("Número da ocorrência inválido.");
        Recorrencia = recorrencia ?? throw new ArgumentNullException(nameof(recorrencia));
        NumeroOcorrencia = numeroOcorrencia;
    }

    // Pendente com data anterior a hoje (Brasília) já fica ATRASADA, sem esperar a verificação
    private static StatusTransacao AjustarAtraso(StatusTransacao status, DateOnly dataTransacao)
        => status == StatusTransacao.PENDENTE && dataTransacao < Relogio.HojeBrasilia()
            ? StatusTransacao.ATRASADA
            : status;

    public void Ativar() => Ativo = true;
    public void Inativar() => Ativo = false;
    public override string ToString() => $"[{Id}] {Descricao}";
}