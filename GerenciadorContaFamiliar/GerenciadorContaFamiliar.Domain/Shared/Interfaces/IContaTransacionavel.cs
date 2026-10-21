using GerenciadorContaFamiliar.Domain;

namespace GerenciadorContaFamiliar.Domain.Shared.Interfaces;

public interface IContaTransacionavel
{
    void EfetuarTransacao(Transacao transacao);
    void EstornarTransacao(Transacao transacao);

    /// <summary>Soma a transação ao saldo (usado quando ela passa a CONCLUÍDA).</summary>
    void AplicarNoSaldo(Transacao transacao);

    /// <summary>Tira a transação do saldo (usado quando ela deixa de ser CONCLUÍDA).</summary>
    void DesfazerNoSaldo(Transacao transacao);

    decimal Saldo { get; }
}
