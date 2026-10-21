using System.ComponentModel.DataAnnotations;

namespace GerenciadorContaFamiliar.Application.DTOs.Requests.Recorrencias;

public class PararRecorrenciaRequest
{
    /// <summary>Uma ocorrência da série (vem da rota).</summary>
    [Range(1, int.MaxValue, ErrorMessage = "ID inválido")]
    public int TransacaoId { get; set; }

    /// <summary>Injetado automaticamente pelo controller a partir do JWT.</summary>
    public int MembroId { get; set; }
}
