using GerenciadorContaFamiliar.Domain;

namespace GerenciadorContaFamiliar.Domain.Shared.Interfaces;

public interface ICodigoRecuperacaoSenhaRepository
{
    /// <summary>Código mais recente ainda não usado do membro (só leitura), ou null.</summary>
    Task<CodigoRecuperacaoSenha?> ObterUltimoDoMembroAsync(int membroId, CancellationToken ct = default);

    /// <summary>Quantos códigos o membro pediu desde o instante informado (limite por conta).</summary>
    Task<int> ContarCriadosDesdeAsync(int membroId, DateTime desdeUtc, CancellationToken ct = default);

    /// <summary>
    /// Gasta uma tentativa do código direto no banco, só se ele ainda vale (atômico: palpites em
    /// paralelo não passam do limite). False = não há tentativa disponível.
    /// </summary>
    Task<bool> ReservarTentativaAsync(int codigoId, DateTime agoraUtc, CancellationToken ct = default);

    /// <summary>Marca o código como usado só se ainda não foi (atômico). False = outro pedido já usou.</summary>
    Task<bool> MarcarUsadoSeAbertoAsync(int codigoId, CancellationToken ct = default);

    /// <summary>Marca como usados os códigos em aberto do membro (um pedido novo anula os anteriores).</summary>
    Task InvalidarDoMembroAsync(int membroId, CancellationToken ct = default);

    Task AdicionarAsync(CodigoRecuperacaoSenha codigo, CancellationToken ct = default);
}
