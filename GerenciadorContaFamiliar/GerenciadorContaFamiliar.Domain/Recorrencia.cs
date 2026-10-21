using GerenciadorContaFamiliar.Domain.Shared;
namespace GerenciadorContaFamiliar.Domain;

/// <summary>
/// Molde de uma série recorrente (mensal ou semanal). Cada ocorrência gerada é uma
/// <see cref="Transacao"/> com os dados copiados daqui.
/// </summary>
public class Recorrencia : BaseEntity
{
    public const int MinimoOcorrencias = 2;
    public const int MaximoOcorrencias = 120;

    public decimal Valor { get; private set; }
    public string? Descricao { get; private set; }
    public Transacao.TipoTransacao Tipo { get; private set; }
    public Transacao.ClassificacaoTransacao Classificacao { get; private set; }
    public Transacao.PeriodicidadeTransacao Periodicidade { get; private set; }
    public Membro Membro { get; private set; }
    public Conta? Conta { get; private set; }
    public ContaConjunta? ContaConjunta { get; private set; }
    public Categoria? Categoria { get; private set; }
    public SubCategoria? SubCategoria { get; private set; }
    public DateOnly DataInicio { get; private set; }
    /// <summary>Dia do mês das ocorrências mensais (o da <see cref="DataInicio"/>).</summary>
    public int DiaDoMes { get; private set; }
    public DateOnly ProximaData { get; private set; }
    /// <summary>Quantidade de vezes da série; null = sem fim.</summary>
    public int? TotalOcorrencias { get; private set; }
    /// <summary>Ocorrências já geradas, incluindo a primeira.</summary>
    public int OcorrenciasGeradas { get; private set; }
    /// <summary>false = a série parou de repetir.</summary>
    public bool Ativa { get; private set; }
    public DateTime CriadoEm { get; private set; }

    // Construtor privado usado pelo EF Core ao materializar entidades do banco.
#pragma warning disable CS8618
    private Recorrencia() { }
#pragma warning restore CS8618

    /// <summary>
    /// Cria a série a partir da sua primeira ocorrência, que já conta como gerada.
    /// </summary>
    public Recorrencia(Transacao primeiraOcorrencia, int? totalOcorrencias)
    {
        if (primeiraOcorrencia == null)
            throw new ArgumentNullException(nameof(primeiraOcorrencia));
        if (!primeiraOcorrencia.Recorrente || !primeiraOcorrencia.Periodicidade.HasValue)
            throw new ArgumentException("A transação precisa ser recorrente e ter periodicidade.");
        if (totalOcorrencias.HasValue &&
            (totalOcorrencias < MinimoOcorrencias || totalOcorrencias > MaximoOcorrencias))
            throw new ArgumentException(
                $"A quantidade de vezes deve ficar entre {MinimoOcorrencias} e {MaximoOcorrencias}.");

        Valor = primeiraOcorrencia.Valor;
        Descricao = primeiraOcorrencia.Descricao;
        Tipo = primeiraOcorrencia.Tipo;
        Classificacao = primeiraOcorrencia.Classificacao;
        Periodicidade = primeiraOcorrencia.Periodicidade.Value;
        Membro = primeiraOcorrencia.Membro;
        Conta = primeiraOcorrencia.Conta;
        ContaConjunta = primeiraOcorrencia.ContaConjunta;
        Categoria = primeiraOcorrencia.Categoria;
        SubCategoria = primeiraOcorrencia.SubCategoria;
        DataInicio = primeiraOcorrencia.DataTransacao;
        DiaDoMes = DataInicio.Day;
        TotalOcorrencias = totalOcorrencias;
        OcorrenciasGeradas = 1;
        ProximaData = CalcularDataSeguinte(DataInicio);
        Ativa = true;
        CriadoEm = DateTime.UtcNow;
    }

    public bool AtingiuTotal => TotalOcorrencias.HasValue && OcorrenciasGeradas >= TotalOcorrencias.Value;

    /// <summary>
    /// Registra a geração da ocorrência de <see cref="ProximaData"/>: avança a próxima data e
    /// encerra a série ao atingir a quantidade de vezes. Devolve o número da ocorrência.
    /// </summary>
    public int RegistrarOcorrencia()
    {
        if (!Ativa)
            throw new InvalidOperationException("A série não está ativa.");
        if (AtingiuTotal)
            throw new InvalidOperationException("A série já atingiu a quantidade de vezes.");

        OcorrenciasGeradas++;
        ProximaData = CalcularDataSeguinte(ProximaData);
        if (AtingiuTotal)
            Ativa = false;
        return OcorrenciasGeradas;
    }

    /// <summary>
    /// Status de uma ocorrência gerada: datas anteriores ao dia em que a série foi criada são
    /// histórico já pago (CONCLUIDA); a partir dele, PENDENTE (vira ATRASADA se passar do dia).
    /// </summary>
    public Transacao.StatusTransacao StatusDaOcorrencia(DateOnly data)
        => data < Relogio.DataBrasilia(CriadoEm)
            ? Transacao.StatusTransacao.CONCLUIDA
            : Transacao.StatusTransacao.PENDENTE;

    /// <summary>
    /// Próximas ocorrências ainda não geradas, até a data informada, com o número de cada uma
    /// (respeita a quantidade de vezes). Só calcula: não altera a série.
    /// </summary>
    public IReadOnlyList<(DateOnly Data, int Numero)> ProximasOcorrencias(DateOnly ate)
    {
        var proximas = new List<(DateOnly, int)>();
        if (!Ativa)
            return proximas;
        var data = ProximaData;
        var numero = OcorrenciasGeradas + 1;
        while (data <= ate && (!TotalOcorrencias.HasValue || numero <= TotalOcorrencias.Value))
        {
            proximas.Add((data, numero));
            data = CalcularDataSeguinte(data);
            numero++;
        }
        return proximas;
    }

    /// <summary>Para de repetir; as ocorrências já geradas ficam como estão.</summary>
    public void Parar() => Ativa = false;

    // Semanal: +7 dias. Mensal: mesmo dia do mês; sem esse dia no mês (31 em abril,
    // 30/31 em fevereiro), usa o último dia e volta ao dia original no mês seguinte.
    private DateOnly CalcularDataSeguinte(DateOnly data)
    {
        if (Periodicidade == Transacao.PeriodicidadeTransacao.SEMANAL)
            return data.AddDays(7);

        var mesSeguinte = new DateOnly(data.Year, data.Month, 1).AddMonths(1);
        var dia = Math.Min(DiaDoMes, DateTime.DaysInMonth(mesSeguinte.Year, mesSeguinte.Month));
        return new DateOnly(mesSeguinte.Year, mesSeguinte.Month, dia);
    }

    public override string ToString() => $"[{Id}] {Descricao}";
}
