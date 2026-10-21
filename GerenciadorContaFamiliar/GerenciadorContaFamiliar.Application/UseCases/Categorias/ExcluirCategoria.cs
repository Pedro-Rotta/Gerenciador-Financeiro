using GerenciadorContaFamiliar.Application.DTOs.Requests.Categorias;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Categorias;
using GerenciadorContaFamiliar.Application.UseCases.Notificacoes;
using GerenciadorContaFamiliar.Application.Exceptions;
namespace GerenciadorContaFamiliar.Application.UseCases.Categorias;

public class ExcluirCategoriaUseCase : IExcluirCategoriaUseCase
{
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly IContaRepository _contaRepository;
    private readonly IContaConjuntaRepository _contaConjuntaRepository;
    private readonly INotificacaoRepository _notificacaoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ExcluirCategoriaUseCase(
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
        ExcluirCategoriaRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // 1. Buscar a entidade real pelo Id
        var categoria = await _categoriaRepository.ObterPorIdAsync(request.Id, ct)
            ?? throw new KeyNotFoundException("Categoria não encontrada.");

        // 0. Verificar propriedade: categoria pertence a Conta do membro ou ContaConjunta do membro
        ContaConjunta? contaConjunta = null;
        if (categoria.ContaId.HasValue)
        {
            var conta = await _contaRepository.ObterPorIdAsync(categoria.ContaId.Value, ct);
            if (conta is null || conta.MembroId != request.MembroId)
                throw new AcessoNegadoException("Você não tem permissão para excluir esta categoria.");
        }
        else if (categoria.ContaConjuntaId.HasValue)
        {
            contaConjunta = await _contaConjuntaRepository.ObterPorIdAsync(categoria.ContaConjuntaId.Value, ct);
            if (contaConjunta is null || !contaConjunta.Membros.Any(m => m.Id == request.MembroId))
                throw new AcessoNegadoException("Você não tem permissão para excluir esta categoria.");
        }
        else
        {
            throw new InvalidOperationException("Categoria não está vinculada a nenhuma conta.");
        }

        if (!categoria.Ativo)
            throw new InvalidOperationException("Categoria já está inativa.");
            
        // 2. Atualizar o categoria encontrado
        categoria.Inativar();

        // Avisar os outros participantes da conta conjunta
        if (contaConjunta is not null)
        {
            var autor = contaConjunta.Membros.First(m => m.Id == request.MembroId);
            await AvisosNotificacao.AvisarOutrosParticipantesAsync(
                _notificacaoRepository, contaConjunta, autor.Id,
                $"{autor.Nome} excluiu a categoria \"{categoria.Nome}\" em \"{contaConjunta.Nome}\".",
                Notificacao.GravidadeNotificacao.ATENCAO, ct);
        }

        // 3. Persistir
        await _categoriaRepository.AtualizarAsync(categoria, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // 4. Mapear resposta
        return new CategoriaResponse
        {
            Id = categoria.Id, 
            Nome = categoria.Nome, 
            Tipo = categoria.Tipo,
            Necessario = categoria.Necessario, 
            Icone = categoria.Icone,
            Ativo = categoria.Ativo,
            ContaId = categoria.ContaId,
            ContaConjuntaId = categoria.ContaConjuntaId,
        };
    }
}