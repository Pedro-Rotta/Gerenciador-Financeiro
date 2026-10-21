namespace GerenciadorContaFamiliar.Application.DTOs.Responses;

public class MetaTransacaoResponse
{
    public int Id { get; set; }
    public int MetaId { get; set; }
    public int TransacaoId { get; set; }
    public DateTime VinculadoEm { get; set; }
}
