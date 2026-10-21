using GerenciadorContaFamiliar.Application.DTOs.Requests.Contas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Contas;
using GerenciadorContaFamiliar.Application.Exceptions;

namespace GerenciadorContaFamiliar.Application.UseCases.Contas;

public class AlterarContaPrincipalUseCase : IAlterarContaPrincipalUseCase
{
    private readonly IContaRepository _contaRepository;
    private readonly IMembroRepository _membroRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AlterarContaPrincipalUseCase(
        IContaRepository contaRepository,
        IMembroRepository membroRepository,
        IUnitOfWork unitOfWork)
    {
        _contaRepository = contaRepository ?? throw new ArgumentNullException(nameof(contaRepository));
        _membroRepository = membroRepository ?? throw new ArgumentNullException(nameof(membroRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ContaResponse> ExecutarAsync(
        AlterarContaPrincipalRequest request,
        CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // 1. Buscar a nova conta principal
        var novaPrincipal = await _contaRepository.ObterPorIdAsync(request.Id, ct)
            ?? throw new KeyNotFoundException("Conta não encontrada.");

        // 2. Verificar propriedade: só o dono pode alterar
        if (novaPrincipal.MembroId != request.MembroId)
            throw new AcessoNegadoException("Você não tem permissão para alterar esta conta.");

        if (!novaPrincipal.Ativo)
            throw new InvalidOperationException("Uma conta inativa não pode ser a principal.");

        if (novaPrincipal.ContaPrincipal)
            throw new InvalidOperationException("Esta conta já é a principal.");

        // 3. Destituir a conta principal atual
        var principalAtual = await _contaRepository.ObterContaPrincipalAsync(request.MembroId, ct);
        if (principalAtual is not null)
        {
            principalAtual.DestituirDePrincipal();
            await _contaRepository.AtualizarAsync(principalAtual, ct);
        }

        // 4. Marcar a nova conta como principal
        novaPrincipal.MarcarComoPrincipal();
        await _contaRepository.AtualizarAsync(novaPrincipal, ct);

        // 5. Atualizar o vínculo no membro
        var membro = await _membroRepository.ObterPorIdAsync(request.MembroId, ct)
            ?? throw new KeyNotFoundException("Membro não encontrado.");

        membro.DefinirContaPrincipal(novaPrincipal.Id);
        await _membroRepository.AtualizarAsync(membro, ct);

        // 6. Persistir
        await _unitOfWork.SaveChangesAsync(ct);

        // 7. Mapear resposta
        return new ContaResponse
        {
            Id = novaPrincipal.Id,
            Nome = novaPrincipal.Nome,
            Saldo = novaPrincipal.Saldo,
            SaldoInicialDefinido = novaPrincipal.SaldoInicialDefinido,
            MembroId = novaPrincipal.MembroId,
            Ativo = novaPrincipal.Ativo,
            ContaPrincipal = novaPrincipal.EhContaPrincipal,
        };
    }
}
