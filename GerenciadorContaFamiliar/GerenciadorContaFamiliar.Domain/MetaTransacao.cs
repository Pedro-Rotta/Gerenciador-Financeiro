using GerenciadorContaFamiliar.Domain.Shared;

namespace GerenciadorContaFamiliar.Domain;

/// <summary>
/// Tabela de junção entre Meta e Transacao.
/// Permite rastrear quais transações contribuíram para o progresso de uma meta.
/// </summary>
public class MetaTransacao : BaseEntity
{
    public int MetaId { get; private set; }
    public int TransacaoId { get; private set; }
    public DateTime VinculadoEm { get; private set; }

    // Construtor privado usado pelo EF Core ao materializar entidades do banco.
#pragma warning disable CS8618
    private MetaTransacao() { }
#pragma warning restore CS8618

    public MetaTransacao(int metaId, int transacaoId)
    {
        if (metaId <= 0)
            throw new ArgumentException("O ID da meta é obrigatório.");
        if (transacaoId <= 0)
            throw new ArgumentException("O ID da transação é obrigatório.");

        MetaId = metaId;
        TransacaoId = transacaoId;
        VinculadoEm = DateTime.UtcNow;
    }

    public override string ToString() => $"Meta #{MetaId} <-> Transacao #{TransacaoId}";
}
