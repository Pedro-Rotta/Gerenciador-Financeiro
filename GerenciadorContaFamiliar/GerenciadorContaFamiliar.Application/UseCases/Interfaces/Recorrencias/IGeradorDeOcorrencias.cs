using GerenciadorContaFamiliar.Domain;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Recorrencias;

/// <summary>
/// Gera as ocorrências de uma série até a antecedência permitida (lança na conta e roda os
/// avisos). Usado ao criar a série e na verificação ao abrir o app; quem chama grava.
/// </summary>
public interface IGeradorDeOcorrencias
{
    Task GerarAsync(Recorrencia serie, CancellationToken ct = default);
}
