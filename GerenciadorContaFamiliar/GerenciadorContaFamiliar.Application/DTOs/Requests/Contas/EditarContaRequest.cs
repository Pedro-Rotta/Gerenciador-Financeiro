using System.ComponentModel.DataAnnotations;
using GerenciadorContaFamiliar.Domain;
namespace GerenciadorContaFamiliar.Application.DTOs.Requests.Contas;

public class EditarContaRequest
{
    /// <summary>Injetado automaticamente pelo controller a partir da URL.</summary>
    public int Id { get; set; }

    [StringLength(100, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 100 caracteres")]
    public string? Nome { get; set; }

    /// <summary>Injetado automaticamente pelo controller a partir do JWT.</summary>
    public int MembroId { get; set; }
}