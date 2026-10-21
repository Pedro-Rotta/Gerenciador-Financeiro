using GerenciadorContaFamiliar.Domain.Shared;
namespace GerenciadorContaFamiliar.Domain;

public class Notificacao : BaseEntity
{
    // Tipo define o ícone e Gravidade a cor do ícone (INFORMATIVA = verde, ATENCAO = amarelo, CRITICA = vermelho)
    public enum TipoNotificacao { GERAL, CONTA_CONJUNTA, TRANSACAO, SALDO, META }
    public enum GravidadeNotificacao { INFORMATIVA, ATENCAO, CRITICA }

    public string Mensagem { get; private set; }
    public bool Lida { get; private set; }
    public TipoNotificacao Tipo { get; private set; }
    public GravidadeNotificacao Gravidade { get; private set; }
    public Membro Membro { get; private set; }
    /// <summary>
    /// Identifica um aviso que só pode existir uma vez por membro (ex.: "vencimento:15").
    /// null = aviso comum, sem anti-repetição.
    /// </summary>
    public string? Chave { get; private set; }
    public DateTime CriadoEm { get; private set; }

    public const int TamanhoMaximoChave = 100;

    // Construtor privado usado pelo EF Core ao materializar entidades do banco.
#pragma warning disable CS8618
    private Notificacao() { }
#pragma warning restore CS8618

    public Notificacao(
        string mensagem,
        Membro membro,
        TipoNotificacao tipo = TipoNotificacao.GERAL,
        GravidadeNotificacao gravidade = GravidadeNotificacao.INFORMATIVA,
        string? chave = null)
    {
        if (string.IsNullOrWhiteSpace(mensagem))
            throw new ArgumentException("Mensagem é obrigatória");
        if (membro == null)
            throw new ArgumentNullException(nameof(membro));
        if (chave is not null && (string.IsNullOrWhiteSpace(chave) || chave.Length > TamanhoMaximoChave))
            throw new ArgumentException("Chave da notificação inválida.");
        Mensagem = mensagem;
        Membro = membro;
        Tipo = tipo;
        Gravidade = gravidade;
        Chave = chave;
        Lida = false;
        CriadoEm = DateTime.UtcNow;
    }
    public void AtualizarMensagem(string mensagem)
    {
        if (string.IsNullOrWhiteSpace(mensagem))
            throw new ArgumentException("Mensagem é obrigatória");
        Mensagem = mensagem;
    }
    public void MarcarComoLida() => Lida = true;
    public override string ToString() => $"[{Id}] {Mensagem}";
}