using System.ComponentModel.DataAnnotations;


namespace GerenciadorContaFamiliar.Application.DTOs.Requests.ContasConjuntas;

public class ExcluirContaConjuntaRequest
{
    [Required(ErrorMessage = "O ID é obrigatório")]
    [Range(1, int.MaxValue, ErrorMessage = "ID inválido")]
    public int Id { get; set; }

    /// <summary>Injetado automaticamente pelo controller a partir do JWT.</summary>
    public int MembroId { get; set; }
}