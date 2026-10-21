namespace GerenciadorContaFamiliar.Application.DTOs.Responses;

public class ContaConjuntaResponse
{
    public int Id { get; set; }
    public string Nome { get; set; } = null!;
    /// <summary>Saldo real: só o que foi pago/recebido.</summary>
    public decimal Saldo { get; set; }

    /// <summary>Saldo + pendentes/atrasadas (entradas − saídas). Preenchido só na listagem.</summary>
    public decimal? SaldoPrevisto { get; set; }
    public string? Descricao { get; set; }
    public int CriadorId { get; set; }
    public bool Ativo { get; set; }
    public IReadOnlyList<MembroResumoResponse> Membros { get; set; } = new List<MembroResumoResponse>();
}
