using System.ComponentModel.DataAnnotations;

namespace GerenciadorContaFamiliar.Application.DTOs.Requests.ContasConjuntas;

public class EditarContaConjuntaRequest
{
    /// <summary>Injetado automaticamente pelo controller a partir da URL.</summary>
    public int Id { get; set; }
    [StringLength(100, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 100 caracteres")]
    public string? Nome { get; set; }

    /// <summary>Opcional. Enviar "" (vazio) limpa a descrição.</summary>
    [StringLength(100, ErrorMessage = "A descricao deve ter no máximo 100 caracteres")]
    public string? Descricao { get; set; }

    /// <summary>Injetado automaticamente pelo controller a partir do JWT.</summary>
    public int MembroId { get; set; }
}