using GerenciadorContaFamiliar.Domain;

namespace GerenciadorContaFamiliar.Application.DTOs.Responses;

public class NotificacaoResponse
{
    public int Id { get; set; }
    public string Mensagem { get; set; } = null!;
    public bool Lida { get; set; }
    public Notificacao.TipoNotificacao Tipo { get; set; }
    public Notificacao.GravidadeNotificacao Gravidade { get; set; }
    public int MembroId { get; set; }
    public DateTime CriadoEm { get; set; }
}
