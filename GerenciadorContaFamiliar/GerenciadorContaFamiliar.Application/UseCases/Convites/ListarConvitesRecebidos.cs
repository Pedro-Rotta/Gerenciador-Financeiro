using GerenciadorContaFamiliar.Application.DTOs.Requests.Convites;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Convites;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;

namespace GerenciadorContaFamiliar.Application.UseCases.Convites;

/// <summary>
/// Lista os convites PENDENTES recebidos pelo membro logado (exibidos no sino de notificações).
/// </summary>
public class ListarConvitesRecebidosUseCase : IListarConvitesRecebidosUseCase
{
    private readonly IConviteContaConjuntaRepository _conviteRepository;

    public ListarConvitesRecebidosUseCase(IConviteContaConjuntaRepository conviteRepository)
    {
        _conviteRepository = conviteRepository ?? throw new ArgumentNullException(nameof(conviteRepository));
    }

    public async Task<IEnumerable<ConviteResponse>> ExecutarAsync(
        ListarConvitesRecebidosRequest request,
        CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var convites = await _conviteRepository.ObterPendentesDoConvidadoAsync(request.MembroId, ct);

        return convites.Select(MapearParaResponse);
    }

    private static ConviteResponse MapearParaResponse(ConviteContaConjunta c) => new()
    {
        Id = c.Id,
        ContaConjuntaId = c.ContaConjuntaId,
        ContaConjuntaNome = c.ContaConjunta.Nome,
        ConvidadoId = c.ConvidadoId,
        ConvidadoNome = c.Convidado.Nome,
        ConvidadoNomeDeUsuario = c.Convidado.NomeDeUsuario,
        ConvidadoPorId = c.ConvidadoPorId,
        ConvidadoPorNome = c.ConvidadoPor.Nome,
        Status = c.Status,
        CriadoEm = c.CriadoEm,
        RespondidoEm = c.RespondidoEm,
    };
}
