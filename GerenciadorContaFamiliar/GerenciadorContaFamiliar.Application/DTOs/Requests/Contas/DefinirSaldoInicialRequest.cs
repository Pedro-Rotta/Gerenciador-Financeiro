namespace GerenciadorContaFamiliar.Application.DTOs.Requests.Contas;

public class DefinirSaldoInicialRequest
{
    /// <summary>Saldo da conta antes da primeira transação (pode ser negativo ou zero).</summary>
    public decimal Valor { get; set; }

    /// <summary>Injetado automaticamente pelo controller a partir da URL.</summary>
    public int Id { get; set; }

    /// <summary>Injetado automaticamente pelo controller a partir do JWT.</summary>
    public int MembroId { get; set; }
}
