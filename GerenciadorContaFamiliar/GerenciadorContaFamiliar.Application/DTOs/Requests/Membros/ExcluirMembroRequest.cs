using System.ComponentModel.DataAnnotations;

namespace GerenciadorContaFamiliar.Application.DTOs.Requests.Membros;

/// <summary>DELETE api/Membro — excluir a própria conta, confirmando com a senha.</summary>
public class ExcluirMembroRequest
{
    [Required(ErrorMessage = "Informe a sua senha para confirmar")]
    public string Senha { get; set; } = string.Empty;

    /// <summary>Injetado automaticamente pelo controller a partir do JWT.</summary>
    public int MembroId { get; set; }
}
