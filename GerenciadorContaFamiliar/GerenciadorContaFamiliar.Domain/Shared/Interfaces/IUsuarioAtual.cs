namespace GerenciadorContaFamiliar.Domain.Shared.Interfaces;

/// <summary>
/// Abstração do membro autenticado na requisição atual.
/// A implementação concreta (leitura do JWT) fica na Web API.
/// </summary>
public interface IUsuarioAtual
{
    /// <summary>
    /// ID do membro logado, ou null fora de uma requisição autenticada
    /// (ex.: cadastro, login, migrations).
    /// </summary>
    int? MembroId { get; }
}
