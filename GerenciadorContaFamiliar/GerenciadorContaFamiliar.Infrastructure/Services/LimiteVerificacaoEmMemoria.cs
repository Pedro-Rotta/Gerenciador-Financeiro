using System.Collections.Concurrent;
using GerenciadorContaFamiliar.Domain.Shared;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;

namespace GerenciadorContaFamiliar.Infrastructure.Services;

/// <summary>
/// Controle em memória (singleton) da última verificação de cada membro: roda de novo depois de
/// 1 hora ou quando muda o dia em Brasília (para a virada do dia valer na hora). Se a API reiniciar,
/// verifica de novo — sem problema, porque os passos não repetem o que já foi feito.
/// </summary>
public class LimiteVerificacaoEmMemoria : ILimiteVerificacao
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(1);
    private readonly ConcurrentDictionary<int, DateTime> _ultimaVerificacao = new();

    public bool TentarReservar(int membroId)
    {
        var agora = DateTime.UtcNow;
        while (true)
        {
            if (!_ultimaVerificacao.TryGetValue(membroId, out var ultima))
            {
                if (_ultimaVerificacao.TryAdd(membroId, agora))
                    return true;
                continue;
            }
            if (agora - ultima < Intervalo && Relogio.DataBrasilia(ultima) == Relogio.DataBrasilia(agora))
                return false;
            // Troca só se ninguém reservou no meio tempo (duas abas ao mesmo tempo)
            if (_ultimaVerificacao.TryUpdate(membroId, agora, ultima))
                return true;
        }
    }
}
