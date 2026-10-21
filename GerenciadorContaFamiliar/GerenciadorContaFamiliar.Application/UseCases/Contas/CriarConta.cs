using System.Collections.Generic;
using GerenciadorContaFamiliar.Application.DTOs.Requests.Categorias;
using GerenciadorContaFamiliar.Application.DTOs.Requests.Contas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Categorias;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Categorias;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Contas;


namespace GerenciadorContaFamiliar.Application.UseCases.Contas;

public class CriarContaUseCase : ICriarContaUseCase
{
    private readonly IContaRepository _contaRepository;
    private readonly IMembroRepository _membroRepository;
    private readonly ICriarCategoriaUseCase _criarCategoriaUseCase;
    private readonly IUnitOfWork _unitOfWork;

    public CriarContaUseCase(
        IContaRepository contaRepository,
        IMembroRepository membroRepository,
        ICriarCategoriaUseCase criarCategoriaUseCase,
        IUnitOfWork unitOfWork)
    {
        _contaRepository = contaRepository ?? throw new ArgumentNullException(nameof(contaRepository));
        _membroRepository = membroRepository ?? throw new ArgumentNullException(nameof(membroRepository));
        _criarCategoriaUseCase = criarCategoriaUseCase ?? throw new ArgumentNullException(nameof(criarCategoriaUseCase));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ContaResponse> ExecutarAsync(
        CriarContaRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // 1️⃣ Se for conta principal, garante que o membro ainda não possui uma
        if (request.ContaPrincipal)
        {
            var principalExistente = await _contaRepository.ObterContaPrincipalAsync(request.MembroId, cancellationToken);
            if (principalExistente is not null)
                throw new InvalidOperationException("O membro já possui uma conta principal.");
        }

        // 2️⃣ Criar entidade de domínio
        var conta = new Conta(
            request.Nome,
            request.MembroId,
            request.ContaPrincipal
        );

        // 3️⃣ Persistir a conta — observação sobre transação:
        await _contaRepository.AdicionarAsync(conta, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken); // conta.Id gerado aqui

        // 4️⃣ Criar as categorias padrão da conta
        foreach (var categoria in CategoriasPadrao.Criar())
        {
            await _criarCategoriaUseCase.ExecutarAsync(new CriarCategoriaRequest
            {
                Nome = categoria.Nome,
                Tipo = categoria.Tipo,
                Necessario = categoria.Necessario,
                Icone = categoria.Icone,
                ContaId = conta.Id,
                MembroId = request.MembroId
            }, cancellationToken);
        }

        // 5️⃣ Se for a conta principal, vincula ao membro (define Membro.ContaPrincipalId)
        if (request.ContaPrincipal)
        {
            var membro = await _membroRepository.ObterPorIdAsync(request.MembroId, cancellationToken)
                ?? throw new KeyNotFoundException("Membro não encontrado.");

            membro.DefinirContaPrincipal(conta.Id);
            await _membroRepository.AtualizarAsync(membro, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // 6️⃣ Mapear para DTO de saída
        return new ContaResponse
        {
            Id = conta.Id,
            Nome = conta.Nome,
            Saldo = conta.Saldo,
            SaldoInicialDefinido = conta.SaldoInicialDefinido,
            Ativo = conta.Ativo,
            MembroId = conta.MembroId,
            ContaPrincipal = conta.EhContaPrincipal,
        };
    }
}
