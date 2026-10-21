using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;

namespace GerenciadorContaFamiliar.Application.UseCases.Auth;

/// <summary>Busca da conta pelo identificador do "Esqueceu a senha": e-mail (com @) ou nome de usuário.</summary>
internal static class ContaDoIdentificador
{
    public static async Task<Membro?> AcharAsync(IMembroRepository membros, string identificador, CancellationToken ct)
    {
        var texto = identificador.Trim();
        // Nome de usuário diferencia maiúsculas (como no login); e-mail não. Com "@" tenta o
        // e-mail primeiro e, se não achar, o nome de usuário (o cadastro aceita "@" no nome)
        if (texto.Contains('@'))
            return await membros.ObterPorEmailAsync(texto, ct)
                ?? await membros.ObterPorNomeDeUsuarioAsync(texto, ct);
        return await membros.ObterPorNomeDeUsuarioAsync(texto, ct);
    }
}
