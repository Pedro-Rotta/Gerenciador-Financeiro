using GerenciadorContaFamiliar.Domain.Shared;

namespace GerenciadorContaFamiliar.Domain;

/// <summary>
/// Convite para um membro participar de uma ContaConjunta.
/// Só o criador da conta convida; o convidado aceita ou recusa (sem segundo aceite).
/// Ao aceitar, o convidado passa a ser participante da conta conjunta.
/// </summary>
public class ConviteContaConjunta : BaseEntity
{
    public enum StatusConvite { PENDENTE, ACEITO, RECUSADO, CANCELADO }

    public int ContaConjuntaId { get; private set; }
    public int ConvidadoId { get; private set; }
    public int ConvidadoPorId { get; private set; }
    public StatusConvite Status { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime? RespondidoEm { get; private set; }

    public ContaConjunta ContaConjunta { get; private set; } = null!;
    public Membro Convidado { get; private set; } = null!;
    public Membro ConvidadoPor { get; private set; } = null!;

    // Construtor privado usado pelo EF Core ao materializar entidades do banco.
    private ConviteContaConjunta() { }

    public ConviteContaConjunta(int contaConjuntaId, int convidadoId, int convidadoPorId)
    {
        if (contaConjuntaId <= 0)
            throw new ArgumentException("A conta conjunta é obrigatória.");
        if (convidadoId <= 0)
            throw new ArgumentException("O membro convidado é obrigatório.");
        if (convidadoPorId <= 0)
            throw new ArgumentException("O membro que convida é obrigatório.");
        if (convidadoId == convidadoPorId)
            throw new ArgumentException("Você não pode convidar a si mesmo.");

        ContaConjuntaId = contaConjuntaId;
        ConvidadoId = convidadoId;
        ConvidadoPorId = convidadoPorId;
        Status = StatusConvite.PENDENTE;
        CriadoEm = DateTime.UtcNow;
    }

    public void Aceitar() => Responder(StatusConvite.ACEITO);
    public void Recusar() => Responder(StatusConvite.RECUSADO);
    public void Cancelar() => Responder(StatusConvite.CANCELADO);

    // Um convite só pode ser respondido (ou cancelado) uma vez, enquanto está pendente.
    private void Responder(StatusConvite novoStatus)
    {
        if (Status != StatusConvite.PENDENTE)
            throw new InvalidOperationException("Este convite já foi respondido ou cancelado.");
        Status = novoStatus;
        RespondidoEm = DateTime.UtcNow;
    }

    public override string ToString() => $"[{Id}] ContaConjunta #{ContaConjuntaId} -> Membro #{ConvidadoId} ({Status})";
}
