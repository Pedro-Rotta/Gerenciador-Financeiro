using System.ComponentModel.DataAnnotations;
using GerenciadorContaFamiliar.Domain;

namespace GerenciadorContaFamiliar.Application.DTOs.Requests.Transacoes;

public class EditarTransacaoRequest
{
    /// <summary>Injetado automaticamente pelo controller a partir da URL.</summary>
    public int Id { get; set; }

    [StringLength(500, ErrorMessage = "A descrição deve ter no máximo 500 caracteres")]
    public string? Descricao { get; set; }

    public Transacao.StatusTransacao? Status { get; set; }

    /// <summary>Injetado automaticamente pelo controller a partir do JWT.</summary>
    public int MembroId { get; set; }
}