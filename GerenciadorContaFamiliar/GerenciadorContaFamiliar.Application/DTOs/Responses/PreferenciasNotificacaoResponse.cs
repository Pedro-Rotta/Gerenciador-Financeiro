namespace GerenciadorContaFamiliar.Application.DTOs.Responses;

public class PreferenciasNotificacaoResponse
{
    public bool LancamentosOutros { get; set; }
    public bool Vencimento { get; set; }
    public int DiasAntesVencimento { get; set; }
    public bool Atrasada { get; set; }
    public bool SaldoBaixo { get; set; }
    public decimal? ValorSaldoBaixo { get; set; }
    public bool GastoAcimaNormal { get; set; }
    public bool MetaProgresso { get; set; }
    public bool MetaPrazo { get; set; }
    public bool HorarioSilencio { get; set; }
}
