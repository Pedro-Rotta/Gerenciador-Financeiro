using GerenciadorContaFamiliar.Application.DTOs.Requests.Convites;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Convites;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.Exceptions;

namespace GerenciadorContaFamiliar.Application.UseCases.Convites;

/// <summary>
/// O criador da conta conjunta cancela um convite que ainda está pendente.
/// </summary>
public class CancelarConviteUseCase : ICancelarConviteUseCase
{
    private readonly IConviteContaConjuntaRepository _conviteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelarConviteUseCase(IConviteContaConjuntaRepository conviteRepository, IUnitOfWork unitOfWork)
    {
        _conviteRepository = conviteRepository ?? throw new ArgumentNullException(nameof(conviteRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ConviteResponse> ExecutarAsync(
        CancelarConviteRequest request,
        CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // 1. Buscar o convite
        var convite = await _conviteRepository.ObterPorIdAsync(request.Id, ct)
            ?? throw new KeyNotFoundException("Convite não encontrado.");

        // 0. Verificar propriedade: só o criador da conta conjunta pode cancelar
        if (convite.ContaConjunta.CriadorId != request.MembroId)
            throw new AcessoNegadoException("Você não tem permissão para cancelar este convite.");

        // 2. Cancelar (o domínio garante que o convite ainda está pendente)
        convite.Cancelar();

        // 3. Persistir
        await _conviteRepository.AtualizarAsync(convite, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // 4. Mapear resposta
        return MapearParaResponse(convite);
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
