namespace GerenciadorContaFamiliar.Application.DTOs.Responses;

public class MetaResponse
{
    public int Id { get; set; }
    public string Nome { get; set; } = null!;
    public decimal ValorAlvo { get; set; }
    public decimal ValorAtual { get; set; }
    public DateOnly Prazo { get; set; }
    public bool Concluida { get; set; }
    public int? MembroId { get; set; }
    public bool Ativo { get; set; }
    public IReadOnlyList<MetaTransacaoResponse> TransacoesVinculadas { get; set; } = new List<MetaTransacaoResponse>();
}
