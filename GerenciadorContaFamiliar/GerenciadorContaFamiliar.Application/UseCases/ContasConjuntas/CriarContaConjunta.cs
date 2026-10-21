using GerenciadorContaFamiliar.Application.DTOs.Requests.ContasConjuntas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.DTOs.Requests.Categorias;
using GerenciadorContaFamiliar.Application.UseCases.Categorias;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Categorias;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.ContasConjuntas;


namespace GerenciadorContaFamiliar.Application.UseCases.ContasConjuntas;

public class CriarContaConjuntaUseCase : ICriarContaConjuntaUseCase
{
    private readonly IContaConjuntaRepository _contaConjuntaRepository;
    private readonly IMembroRepository _membroRepository;
    private readonly ICriarCategoriaUseCase _criarCategoriaUseCase;
    private readonly IUnitOfWork _unitOfWork;

    public CriarContaConjuntaUseCase(
        IContaConjuntaRepository contaConjuntaRepository,
        IMembroRepository membroRepository,
        ICriarCategoriaUseCase criarCategoriaUseCase,
        IUnitOfWork unitOfWork)
    {
        _contaConjuntaRepository = contaConjuntaRepository ?? throw new ArgumentNullException(nameof(contaConjuntaRepository));
        _membroRepository = membroRepository ?? throw new ArgumentNullException(nameof(membroRepository));
        _criarCategoriaUseCase = criarCategoriaUseCase ?? throw new ArgumentNullException(nameof(criarCategoriaUseCase));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ContaConjuntaResponse> ExecutarAsync(
        CriarContaConjuntaRequest request, 
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // 1️⃣ Validar que o membro criador existe
        var criador = await _membroRepository.ObterPorIdAsync(request.CriadorId, cancellationToken)
            ?? throw new ArgumentException("Membro criador não encontrado.");

        // 2️⃣ Criar entidade de domínio
        var contaConjunta = new ContaConjunta(
            request.Nome,
            request.Descricao,
            criador.Id
        );

        // 3️⃣ Vincular o criador automaticamente como membro da conta
        contaConjunta.AdicionarMembro(criador);
    
        // 4️⃣ Persistir via repositório
        await _contaConjuntaRepository.AdicionarAsync(contaConjunta, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken); // contaConjunta.Id gerado aqui

        // 5️⃣ Criar as categorias padrão da conta conjunta
        foreach (var categoria in CategoriasPadrao.Criar())
        {
            await _criarCategoriaUseCase.ExecutarAsync(new CriarCategoriaRequest
            {
                Nome = categoria.Nome,
                Tipo = categoria.Tipo,
                Necessario = categoria.Necessario,
                Icone = categoria.Icone,
                ContaConjuntaId = contaConjunta.Id,
                MembroId = criador.Id
            }, cancellationToken);
        }

        // 6️⃣ Mapear para DTO de saída
        return new ContaConjuntaResponse
        {
            Id = contaConjunta.Id,
            Nome = contaConjunta.Nome,
            Saldo = contaConjunta.Saldo,
            Descricao = contaConjunta.Descricao,
            CriadorId = contaConjunta.CriadorId,
            Ativo = contaConjunta.Ativo,
            Membros = contaConjunta.Membros
                .Select(m => new MembroResumoResponse { Id = m.Id, Nome = m.Nome, NomeDeUsuario = m.NomeDeUsuario })
                .ToList(),
        };
    }
}