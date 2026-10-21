using GerenciadorContaFamiliar.Application.DTOs.Requests.Contas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Contas;
using GerenciadorContaFamiliar.Application.Exceptions;

namespace GerenciadorContaFamiliar.Application.UseCases.Contas;

public class ExcluirContaUseCase : IExcluirContaUseCase
{
    private readonly IContaRepository _contaRepository;
    private readonly IRecorrenciaRepository _recorrenciaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ExcluirContaUseCase(
        IContaRepository contaRepository,
        IRecorrenciaRepository recorrenciaRepository,
        IUnitOfWork unitOfWork)
    {
        _contaRepository = contaRepository ?? throw new ArgumentNullException(nameof(contaRepository));
        _recorrenciaRepository = recorrenciaRepository ?? throw new ArgumentNullException(nameof(recorrenciaRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ContaResponse> ExecutarAsync(
        ExcluirContaRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // 1. Buscar a entidade real pelo Id
        var conta = await _contaRepository.ObterPorIdAsync(request.Id, ct)
            ?? throw new KeyNotFoundException("Conta não encontrada.");

        // 0. Verificar propriedade: só o dono pode excluir
        if (conta.MembroId != request.MembroId)
            throw new AcessoNegadoException("Você não tem permissão para excluir esta conta.");

        if (!conta.Ativo)
            throw new InvalidOperationException("Conta já está inativa.");

        // A conta principal não pode ser excluída. Para removê-la é necessário antes
        // alterá-la (marcar outra conta como principal) via AlterarContaPrincipalUseCase.
        if (conta.ContaPrincipal)
            throw new InvalidOperationException("A conta principal não pode ser excluída.");

        // 2. Soft delete (inativa)
        conta.Inativar();

        // As séries recorrentes da conta param de repetir
        foreach (var serie in await _recorrenciaRepository.ObterAtivasPorContaAsync(conta.Id, ct))
            serie.Parar();

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
            Ativo = conta.Ativo
        };
    }
}
