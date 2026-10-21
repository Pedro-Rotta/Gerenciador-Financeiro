namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Verificacao;

/// <summary>
/// Um passo da verificação ao abrir o app. Os passos rodam na ordem em que são registrados
/// no DI; cada um grava o próprio trabalho e a falha de um não impede os seguintes.
/// </summary>
public interface IPassoVerificacao
{
    /// <summary>Nome do passo, usado no log quando ele falha.</summary>
    string Nome { get; }

    Task ExecutarAsync(int membroId, CancellationToken ct = default);
}
