using GerenciadorContaFamiliar.Domain.Shared;
namespace GerenciadorContaFamiliar.Domain;

/// <summary>
/// Preferências de notificação de um membro (uma linha por membro, criada na primeira vez que
/// ele salva). Sem linha, valem os padrões do construtor.
/// </summary>
public class PreferenciaNotificacao : BaseEntity
{
    public static readonly int[] DiasAntesPermitidos = { 1, 3, 7 };

    public int MembroId { get; private set; }
    /// <summary>Lançamentos de outros participantes nas contas conjuntas.</summary>
    public bool LancamentosOutros { get; private set; }
    /// <summary>Lembrete antes do vencimento de uma transação pendente.</summary>
    public bool Vencimento { get; private set; }
    public int DiasAntesVencimento { get; private set; }
    public bool Atrasada { get; private set; }
    public bool SaldoBaixo { get; private set; }
    /// <summary>Limite do saldo baixo; vale para contas individuais e conjuntas.</summary>
    public decimal? ValorSaldoBaixo { get; private set; }
    /// <summary>Gasto de uma categoria 30% acima da média dos 3 meses anteriores.</summary>
    public bool GastoAcimaNormal { get; private set; }
    public bool MetaProgresso { get; private set; }
    public bool MetaPrazo { get; private set; }
    /// <summary>Só salvo: vale para e-mail/navegador quando existirem; não segura o sino.</summary>
    public bool HorarioSilencio { get; private set; }

    // Construtor privado usado pelo EF Core ao materializar entidades do banco.
    private PreferenciaNotificacao() { }

    /// <summary>Preferências padrão do membro.</summary>
    public PreferenciaNotificacao(int membroId)
    {
        if (membroId <= 0)
            throw new ArgumentException("O membro é obrigatório.");
        MembroId = membroId;
        LancamentosOutros = false;
        Vencimento = true;
        DiasAntesVencimento = 3;
        Atrasada = true;
        SaldoBaixo = false;
        ValorSaldoBaixo = null;
        GastoAcimaNormal = false;
        MetaProgresso = true;
        MetaPrazo = true;
        HorarioSilencio = false;
    }

    public void Atualizar(
        bool lancamentosOutros,
        bool vencimento,
        int diasAntesVencimento,
        bool atrasada,
        bool saldoBaixo,
        decimal? valorSaldoBaixo,
        bool gastoAcimaNormal,
        bool metaProgresso,
        bool metaPrazo,
        bool horarioSilencio)
    {
        if (!DiasAntesPermitidos.Contains(diasAntesVencimento))
            throw new ArgumentException("Os dias antes do vencimento devem ser 1, 3 ou 7.");
        if (valorSaldoBaixo.HasValue && valorSaldoBaixo <= 0)
            throw new ArgumentException("O valor do saldo baixo deve ser maior que zero.");
        if (saldoBaixo && !valorSaldoBaixo.HasValue)
            throw new ArgumentException("Informe o valor do saldo baixo.");

        LancamentosOutros = lancamentosOutros;
        Vencimento = vencimento;
        DiasAntesVencimento = diasAntesVencimento;
        Atrasada = atrasada;
        SaldoBaixo = saldoBaixo;
        ValorSaldoBaixo = valorSaldoBaixo;
        GastoAcimaNormal = gastoAcimaNormal;
        MetaProgresso = metaProgresso;
        MetaPrazo = metaPrazo;
        HorarioSilencio = horarioSilencio;
    }

    public override string ToString() => $"[{Id}] Preferências do membro {MembroId}";
}
