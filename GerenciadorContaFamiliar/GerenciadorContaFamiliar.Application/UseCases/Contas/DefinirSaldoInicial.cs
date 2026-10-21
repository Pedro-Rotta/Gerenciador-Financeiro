using GerenciadorContaFamiliar.Application.DTOs.Requests.Contas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Contas;
using GerenciadorContaFamiliar.Application.Exceptions;

namespace GerenciadorContaFamiliar.Application.UseCases.Contas;

/// <summary>
/// Saldo inicial da conta individual: informado uma vez só, antes da primeira transação.
/// </summary>
public class DefinirSaldoInicialUseCase : IDefinirSaldoInicialUseCase
{
    private readonly IContaRepository _contaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DefinirSaldoInicialUseCase(IContaRepository contaRepository, IUnitOfWork unitOfWork)
    {
        _contaRepository = contaRepository ?? throw new ArgumentNullException(nameof(contaRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ContaResponse> ExecutarAsync(
        DefinirSaldoInicialRequest request,
        CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // 1. Buscar a conta
        var conta = await _contaRepository.ObterPorIdAsync(request.Id, ct)
            ?? throw new KeyNotFoundException("Conta não encontrada.");

        // 2. Verificar propriedade: só o dono informa o saldo inicial
        if (conta.MembroId != request.MembroId)
            throw new AcessoNegadoException("Você não tem permissão para alterar esta conta.");

        if (!conta.Ativo)
            throw new InvalidOperationException("Conta inativa não pode receber o saldo inicial.");

        // 3. Definir (o domínio recusa a segunda vez)
        conta.DefinirSaldoInicial(request.Valor);

        // 4. Persistir
        await _contaRepository.AtualizarAsync(conta, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // 5. Mapear resposta
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
