using GerenciadorContaFamiliar.Application.DTOs.Requests.Contas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Contas;
using GerenciadorContaFamiliar.Application.Exceptions;

namespace GerenciadorContaFamiliar.Application.UseCases.Contas;

public class EditarContaUseCase : IEditarContaUseCase
{
    private readonly IContaRepository _contaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public EditarContaUseCase(IContaRepository contaRepository, IUnitOfWork unitOfWork)
    {
        _contaRepository = contaRepository ?? throw new ArgumentNullException(nameof(contaRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ContaResponse> ExecutarAsync(
        EditarContaRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // 1. Buscar a entidade real pelo Id
        var conta = await _contaRepository.ObterPorIdAsync(request.Id, ct)
            ?? throw new KeyNotFoundException("Conta não encontrada.");

        // 0. Verificar propriedade: só o dono pode editar
        if (conta.MembroId != request.MembroId)
            throw new AcessoNegadoException("Você não tem permissão para editar esta conta.");

        // 2. Atualizar somente o que foi enviado
        if (request.Nome is not null)
            conta.AtualizarNome(request.Nome);

        // 3. Persistir
        await _contaRepository.AtualizarAsync(conta, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // 4. Mapear resposta
        return new ContaResponse
        {
            Id = conta.Id,
            Nome = conta.Nome,
            Saldo = conta.Saldo,
            SaldoInicialDefinido = conta.SaldoInicialDefinido,
            MembroId = conta.MembroId,
            Ativo = conta.Ativo,
            ContaPrincipal = conta.EhContaPrincipal,
        };
    }
}
