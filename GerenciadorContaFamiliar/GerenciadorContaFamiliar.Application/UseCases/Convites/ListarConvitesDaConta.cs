using GerenciadorContaFamiliar.Application.DTOs.Requests.Convites;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Convites;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.Exceptions;

namespace GerenciadorContaFamiliar.Application.UseCases.Convites;

/// <summary>
/// Lista os convites PENDENTES de uma conta conjunta. Só o criador da conta pode ver.
/// </summary>
public class ListarConvitesDaContaUseCase : IListarConvitesDaContaUseCase
{
    private readonly IConviteContaConjuntaRepository _conviteRepository;
    private readonly IContaConjuntaRepository _contaConjuntaRepository;

    public ListarConvitesDaContaUseCase(
        IConviteContaConjuntaRepository conviteRepository,
        IContaConjuntaRepository contaConjuntaRepository)
    {
        _conviteRepository = conviteRepository ?? throw new ArgumentNullException(nameof(conviteRepository));
        _contaConjuntaRepository = contaConjuntaRepository ?? throw new ArgumentNullException(nameof(contaConjuntaRepository));
    }

    public async Task<IEnumerable<ConviteResponse>> ExecutarAsync(
        ListarConvitesDaContaRequest request,
        CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // 1. Verificar propriedade: só o criador gerencia os convites da conta
        var contaConjunta = await _contaConjuntaRepository.ObterPorIdAsync(request.ContaConjuntaId, ct)
            ?? throw new KeyNotFoundException("Conta conjunta não encontrada.");

        if (contaConjunta.CriadorId != request.MembroId)
            throw new AcessoNegadoException("Só o criador da conta conjunta pode ver os convites.");

        // 2. Buscar os convites pendentes
        var convites = await _conviteRepository.ObterPendentesDaContaAsync(request.ContaConjuntaId, ct);

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
