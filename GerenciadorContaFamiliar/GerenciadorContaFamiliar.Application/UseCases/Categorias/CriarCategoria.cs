using GerenciadorContaFamiliar.Application.DTOs.Requests.Categorias;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Categorias;
using GerenciadorContaFamiliar.Application.UseCases.Notificacoes;
using GerenciadorContaFamiliar.Application.Exceptions;


namespace GerenciadorContaFamiliar.Application.UseCases.Categorias;

public class CriarCategoriaUseCase : ICriarCategoriaUseCase
{
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly IContaRepository _contaRepository;
    private readonly IContaConjuntaRepository _contaConjuntaRepository;
    private readonly INotificacaoRepository _notificacaoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CriarCategoriaUseCase(
        ICategoriaRepository categoriaRepository,
        IContaRepository contaRepository,
        IContaConjuntaRepository contaConjuntaRepository,
        INotificacaoRepository notificacaoRepository,
        IUnitOfWork unitOfWork)
    {
        _categoriaRepository = categoriaRepository ?? throw new ArgumentNullException(nameof(categoriaRepository));
        _contaRepository = contaRepository ?? throw new ArgumentNullException(nameof(contaRepository));
        _contaConjuntaRepository = contaConjuntaRepository ?? throw new ArgumentNullException(nameof(contaConjuntaRepository));
        _notificacaoRepository = notificacaoRepository ?? throw new ArgumentNullException(nameof(notificacaoRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CategoriaResponse> ExecutarAsync(
        CriarCategoriaRequest request, 
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // Validação XOR: exatamente uma das duas FKs deve ser informada
        if ((request.ContaId.HasValue && request.ContaConjuntaId.HasValue) ||
            (!request.ContaId.HasValue && !request.ContaConjuntaId.HasValue))
            throw new ArgumentException(
                "A categoria deve estar vinculada a exatamente uma Conta (ContaId) ou ContaConjunta (ContaConjuntaId).");

        // 0. Verificar propriedade: a conta precisa ser do membro (ou conjunta da qual ele participa)
        ContaConjunta? contaConjunta = null;
        if (request.ContaId.HasValue)
        {
            var conta = await _contaRepository.ObterPorIdAsync(request.ContaId.Value, cancellationToken)
                ?? throw new ArgumentException("Conta não encontrada.");
            if (conta.MembroId != request.MembroId)
                throw new AcessoNegadoException("Você não tem permissão para criar categorias nesta conta.");
        }
        else
        {
            contaConjunta = await _contaConjuntaRepository.ObterPorIdAsync(request.ContaConjuntaId!.Value, cancellationToken);
            if (contaConjunta is null || !contaConjunta.Membros.Any(m => m.Id == request.MembroId))
                throw new AcessoNegadoException("Você não tem permissão para criar categorias nesta conta conjunta.");
        }

        // 1️⃣ Validação de entrada — verifica duplicidade de nome dentro da MESMA conta
        // (individual via ContaId OU conjunta via ContaConjuntaId).
        var existente = await _categoriaRepository.ObterPorNomeAsync(
            request.Nome,
            request.ContaId,
            request.ContaConjuntaId,
            cancellationToken);
        if (existente is not null)
            throw new ArgumentException("Já existe uma categoria com este nome nesta conta.");
            
        // 2️⃣ Criar entidade de domínio
        var categoria = new Categoria(
            request.Nome,
            request.Tipo,
            request.Necessario,
            request.Icone
        );

        // Vincular à Conta ou ContaConjunta
        if (request.ContaId.HasValue)
            categoria.DefinirConta(request.ContaId.Value);
        else
            categoria.DefinirContaConjunta(request.ContaConjuntaId!.Value);

        // Avisar os outros participantes da conta conjunta
        if (contaConjunta is not null)
        {
            var autor = contaConjunta.Membros.First(m => m.Id == request.MembroId);
            await AvisosNotificacao.AvisarOutrosParticipantesAsync(
                _notificacaoRepository, contaConjunta, autor.Id,
                $"{autor.Nome} criou a categoria \"{categoria.Nome}\" em \"{contaConjunta.Nome}\".",
                Notificacao.GravidadeNotificacao.INFORMATIVA, cancellationToken);
        }

        // 3️⃣ Persistir via repositório
        await _categoriaRepository.AdicionarAsync(categoria, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 4️⃣ Mapear para DTO de saída
        return MapParaResponse(categoria);
    }

    private static CategoriaResponse MapParaResponse(Categoria c) => new()
    {
        Id = c.Id,
        Nome = c.Nome,
        Tipo = c.Tipo,
        Necessario = c.Necessario,
        Icone = c.Icone,
        Ativo = c.Ativo,
        ContaId = c.ContaId,
        ContaConjuntaId = c.ContaConjuntaId,
    };
}