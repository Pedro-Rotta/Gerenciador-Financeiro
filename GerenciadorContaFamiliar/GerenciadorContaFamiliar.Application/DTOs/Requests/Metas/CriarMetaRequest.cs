using System.ComponentModel.DataAnnotations;

namespace GerenciadorContaFamiliar.Application.DTOs.Requests.Metas;

public class CriarMetaRequest
{
    [Required(ErrorMessage = "O nome é obrigatório")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 100 caracteres")]
    public string Nome { get; set; } = null!;

    [Range(0.01, double.MaxValue, ErrorMessage = "O valor alvo deve ser maior que zero")]
    public decimal ValorAlvo { get; set; }

    public DateOnly Prazo { get; set; }

    public int? MembroId { get; set; }
}
