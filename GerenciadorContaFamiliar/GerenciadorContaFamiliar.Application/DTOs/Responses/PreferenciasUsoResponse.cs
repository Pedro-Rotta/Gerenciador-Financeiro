namespace GerenciadorContaFamiliar.Application.DTOs.Responses;

/// <summary>GET/PUT api/Membro/preferencias — preferências de uso do app.</summary>
public class PreferenciasUsoResponse
{
    public bool SemanaComecaNoDomingo { get; set; }
    public string? ContaPadrao { get; set; }
    public int PeriodoRelatorios { get; set; }
    public bool OcultarValores { get; set; }
}
