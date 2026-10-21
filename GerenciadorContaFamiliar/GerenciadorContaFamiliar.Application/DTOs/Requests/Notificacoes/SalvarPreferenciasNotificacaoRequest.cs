using System.ComponentModel.DataAnnotations;

namespace GerenciadorContaFamiliar.Application.DTOs.Requests.Notificacoes;

/// <summary>Corpo completo das preferências (o PUT substitui todas).</summary>
public class SalvarPreferenciasNotificacaoRequest
{
    public bool LancamentosOutros { get; set; }
    public bool Vencimento { get; set; }

    [Required(ErrorMessage = "Os dias antes do vencimento são obrigatórios")]
    public int DiasAntesVencimento { get; set; }

    public bool Atrasada { get; set; }
    public bool SaldoBaixo { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "O valor do saldo baixo deve ser maior que zero")]
    public decimal? ValorSaldoBaixo { get; set; }

    public bool GastoAcimaNormal { get; set; }
    public bool MetaProgresso { get; set; }
    public bool MetaPrazo { get; set; }
    public bool HorarioSilencio { get; set; }

    /// <summary>Injetado automaticamente pelo controller a partir do JWT.</summary>
    public int MembroId { get; set; }
}
