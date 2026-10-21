namespace GerenciadorContaFamiliar.Application.DTOs.Responses;

public class ContaResponse
{
    public int Id { get; set; }
    public string Nome { get; set; } = null!;
    /// <summary>Saldo real: só o que foi pago/recebido.</summary>
    public decimal Saldo { get; set; }

    /// <summary>Saldo + pendentes/atrasadas (entradas − saídas). Preenchido só na listagem.</summary>
    public decimal? SaldoPrevisto { get; set; }

    /// <summary>Falso enquanto a conta não recebeu o saldo inicial (não aceita transações).</summary>
    public bool SaldoInicialDefinido { get; set; }
    public int MembroId { get; set; }
    public bool Ativo { get; set; }
    public bool ContaPrincipal { get; set; }
}
