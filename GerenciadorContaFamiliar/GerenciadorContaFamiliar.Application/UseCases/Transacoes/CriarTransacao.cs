using GerenciadorContaFamiliar.Application.DTOs.Requests.Transacoes;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Transacoes;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Notificacoes;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Recorrencias;
using GerenciadorContaFamiliar.Application.Exceptions;

namespace GerenciadorContaFamiliar.Application.UseCases.Transacoes;

public class CriarTransacaoUseCase : ICriarTransacaoUseCase
{
    private readonly ITransacaoRepository _transacaoRepository;
    private readonly IMembroRepository _membroRepository;
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly ISubCategoriaRepository _subCategoriaRepository;
    private readonly IContaRepository _contaRepository;
    private readonly IContaConjuntaRepository _contaConjuntaRepository;
    private readonly IAvisosDeLancamento _avisosDeLancamento;
    private readonly IRecorrenciaRepository _recorrenciaRepository;
    private readonly IGeradorDeOcorrencias _geradorDeOcorrencias;
    private readonly IUnitOfWork _unitOfWork;

    public CriarTransacaoUseCase(
        ITransacaoRepository transacaoRepository,
        IMembroRepository membroRepository,
        ICategoriaRepository categoriaRepository,
        ISubCategoriaRepository subCategoriaRepository,
        IContaRepository contaRepository,
        IContaConjuntaRepository contaConjuntaRepository,
        IAvisosDeLancamento avisosDeLancamento,
        IRecorrenciaRepository recorrenciaRepository,
        IGeradorDeOcorrencias geradorDeOcorrencias,
        IUnitOfWork unitOfWork)
    {
        _transacaoRepository = transacaoRepository ?? throw new ArgumentNullException(nameof(transacaoRepository));
        _membroRepository = membroRepository ?? throw new ArgumentNullException(nameof(membroRepository));
        _categoriaRepository = categoriaRepository ?? throw new ArgumentNullException(nameof(categoriaRepository));
        _subCategoriaRepository = subCategoriaRepository ?? throw new ArgumentNullException(nameof(subCategoriaRepository));
        _contaRepository = contaRepository ?? throw new ArgumentNullException(nameof(contaRepository));
        _contaConjuntaRepository = contaConjuntaRepository ?? throw new ArgumentNullException(nameof(contaConjuntaRepository));
        _avisosDeLancamento = avisosDeLancamento ?? throw new ArgumentNullException(nameof(avisosDeLancamento));
        _recorrenciaRepository = recorrenciaRepository ?? throw new ArgumentNullException(nameof(recorrenciaRepository));
        _geradorDeOcorrencias = geradorDeOcorrencias ?? throw new ArgumentNullException(nameof(geradorDeOcorrencias));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<TransacaoResponse> ExecutarAsync(
        CriarTransacaoRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // 1️⃣ Validação de entrada: carregar dependências
        var membro = await _membroRepository.ObterPorIdAsync(request.MembroId, ct)
            ?? throw new ArgumentException("Membro não encontrado.");

        if (request.ContaId.HasValue == request.ContaConjuntaId.HasValue)
            throw new ArgumentException("Informe exatamente uma conta: ContaId OU ContaConjuntaId.");

        // Verificar propriedade: a conta precisa ser do membro (ou conjunta da qual ele participa)
        Conta? conta = null;
        ContaConjunta? contaConjunta = null;
        if (request.ContaId is not null)
        {
            conta = await _contaRepository.ObterPorIdAsync(request.ContaId.Value, ct)
                ?? throw new ArgumentException("Conta não encontrada.");
            if (conta.MembroId != request.MembroId)
                throw new AcessoNegadoException("Você não tem permissão para lançar transações nesta conta.");
        }
        else
        {
            contaConjunta = await _contaConjuntaRepository.ObterPorIdAsync(request.ContaConjuntaId!.Value, ct)
                ?? throw new ArgumentException("Conta conjunta não encontrada.");
            if (!contaConjunta.Membros.Any(m => m.Id == request.MembroId))
                throw new AcessoNegadoException("Você não tem permissão para lançar transações nesta conta conjunta.");
        }

        // Categoria e subcategoria precisam pertencer à mesma conta da transação
        Categoria? categoria = null;
        if(request.CategoriaId is not null)
        {
            categoria = await _categoriaRepository.ObterPorIdAsync(request.CategoriaId.Value, ct)
                ?? throw new ArgumentException("Categoria não encontrada.");
            if (!PertenceAConta(categoria.ContaId, categoria.ContaConjuntaId, conta, contaConjunta))
                throw new ArgumentException("A categoria não pertence à conta da transação.");
        }
        SubCategoria? subCategoria = null;
        if (request.SubCategoriaId is not null)
        {
            subCategoria = await _subCategoriaRepository.ObterPorIdAsync(request.SubCategoriaId.Value, ct)
                ?? throw new ArgumentException("Subcategoria não encontrada.");
            if (!PertenceAConta(subCategoria.ContaId, subCategoria.ContaConjuntaId, conta, contaConjunta))
                throw new ArgumentException("A subcategoria não pertence à conta da transação.");
        }

        // 2️⃣ Criar entidade de domínio (valida as regras: conta única, periodicidade, etc.)
        var transacao = new Transacao(
            request.Valor,
            request.Descricao,
            request.DataTransacao,
            request.Tipo,
            request.Classificacao,
            request.Recorrente,
            request.Periodicidade,
            membro,
            categoria,
            subCategoria,
            conta,
            contaConjunta,
            request.Status
        );

        // Recorrente: cria a série e esta transação vira a 1ª ocorrência
        if (!request.Recorrente && request.QuantidadeOcorrencias.HasValue)
            throw new ArgumentException("A quantidade de vezes só vale para transação recorrente.");
        Recorrencia? recorrencia = null;
        if (request.Recorrente)
        {
            recorrencia = new Recorrencia(transacao, request.QuantidadeOcorrencias);
            transacao.VincularARecorrencia(recorrencia, recorrencia.OcorrenciasGeradas);
            await _recorrenciaRepository.AdicionarAsync(recorrencia, ct);
        }

        var saldoAnterior = conta?.Saldo ?? contaConjunta!.Saldo;
        transacao.AplicarEmConta();

        // 3️⃣ Persistir via repositório (os avisos contam com a transação já adicionada)
        await _transacaoRepository.AdicionarAsync(transacao, ct);
        await _avisosDeLancamento.AvisarAsync(transacao, saldoAnterior, geradaPelaSerie: false, ct);

        // Série: gera já as ocorrências seguintes (as anteriores a hoje entram como histórico pago)
        if (recorrencia is not null)
            await _geradorDeOcorrencias.GerarAsync(recorrencia, ct);
        await _unitOfWork.SaveChangesAsync(ct);


        // 4️⃣ Mapear para DTO de saída
        return new TransacaoResponse
        {
            Id = transacao.Id,
            Valor = transacao.Valor,
            Descricao = transacao.Descricao,
            DataTransacao = transacao.DataTransacao,
            Tipo = transacao.Tipo,
            Classificacao = transacao.Classificacao,
            Recorrente = transacao.Recorrente,
            Periodicidade = transacao.Periodicidade,
            Status = transacao.Status,
            MembroId = transacao.Membro.Id,
            CategoriaId = transacao.Categoria?.Id,
            SubCategoriaId = transacao.SubCategoria?.Id,
            ContaId = transacao.Conta?.Id,
            ContaConjuntaId = transacao.ContaConjunta?.Id,
            RecorrenciaId = transacao.Recorrencia?.Id,
            NumeroOcorrencia = transacao.NumeroOcorrencia,
            TotalOcorrencias = transacao.Recorrencia?.TotalOcorrencias,
            SerieAtiva = transacao.Recorrencia?.Ativa,
            CriadoEm = transacao.CriadoEm,
            Ativo = transacao.Ativo
        };
    }

    // Verifica se um item (categoria/subcategoria) pertence à conta individual ou conjunta da transação.
    private static bool PertenceAConta(int? contaIdItem, int? contaConjuntaIdItem, Conta? conta, ContaConjunta? contaConjunta)
    {
        if (conta is not null)
            return contaIdItem == conta.Id;
        return contaConjunta is not null && contaConjuntaIdItem == contaConjunta.Id;
    }
}
