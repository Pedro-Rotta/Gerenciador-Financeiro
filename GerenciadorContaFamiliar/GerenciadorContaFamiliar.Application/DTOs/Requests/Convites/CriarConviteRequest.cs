using System.ComponentModel.DataAnnotations;

namespace GerenciadorContaFamiliar.Application.DTOs.Requests.Convites;

public class CriarConviteRequest
{
    [Required(ErrorMessage = "A conta conjunta é obrigatória")]
    [Range(1, int.MaxValue, ErrorMessage = "ID da conta conjunta inválido")]
    public int ContaConjuntaId { get; set; }

    /// <summary>Nome de usuário ou e-mail do membro convidado.</summary>
    [Required(ErrorMessage = "Informe o nome de usuário ou o e-mail do convidado")]
    [StringLength(100, ErrorMessage = "O nome de usuário ou e-mail deve ter no máximo 100 caracteres")]
    public string UsuarioOuEmail { get; set; } = null!;

    /// <summary>Injetado automaticamente pelo controller a partir do JWT.</summary>
    public int MembroId { get; set; }
}
