using System.ComponentModel.DataAnnotations;

namespace GerenciadorContaFamiliar.Application.DTOs.Requests.Membros;

/// <summary>PUT api/Membro/preferencias — preferências de uso do app.</summary>
public class SalvarPreferenciasUsoRequest
{
    public bool SemanaComecaNoDomingo { get; set; }

    /// <summary>"c:ID" ou "cc:ID"; vazio = a conta principal.</summary>
    [StringLength(30)]
    public string? ContaPadrao { get; set; }

    /// <summary>1, 3, 6 ou 12 meses.</summary>
    public int PeriodoRelatorios { get; set; } = 1;

    public bool OcultarValores { get; set; }

    /// <summary>Injetado automaticamente pelo controller a partir do JWT.</summary>
    public int MembroId { get; set; }
}
