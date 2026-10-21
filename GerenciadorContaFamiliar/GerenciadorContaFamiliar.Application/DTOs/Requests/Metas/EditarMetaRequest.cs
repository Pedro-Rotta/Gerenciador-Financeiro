using System.ComponentModel.DataAnnotations;

namespace GerenciadorContaFamiliar.Application.DTOs.Requests.Metas;

public class EditarMetaRequest
{
    /// <summary>Injetado automaticamente pelo controller a partir da URL.</summary>
    public int Id { get; set; }

    [StringLength(100, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 100 caracteres")]
    public string? Nome { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "O valor alvo deve ser maior que zero")]
    public decimal? ValorAlvo { get; set; }

    public DateOnly? Prazo { get; set; }

    /// <summary>Injetado automaticamente pelo controller a partir do JWT.</summary>
    public int MembroId { get; set; }
}
