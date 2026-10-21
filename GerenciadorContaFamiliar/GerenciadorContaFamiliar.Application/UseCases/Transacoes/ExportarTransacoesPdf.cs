using System.Globalization;
using GerenciadorContaFamiliar.Application.DTOs.Requests.Transacoes;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Application.Exceptions;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Transacoes;

namespace GerenciadorContaFamiliar.Application.UseCases.Transacoes;

/// <summary>
/// PDF das transações com os mesmos filtros da tabela "Transações recentes"
/// (tipo, período, conta e categoria), da mais recente para a mais antiga.
/// Considera as contas individuais do membro e as conjuntas das quais ele participa.
/// </summary>
public class ExportarTransacoesPdfUseCase : IExportarTransacoesPdfUseCase
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    private readonly ITransacaoRepository _transacaoRepository;
    private readonly IMembroRepository _membroRepository;
    private readonly IContaRepository _contaRepository;
    private readonly IContaConjuntaRepository _contaConjuntaRepository;
    private readonly IGeradorPdfTransacoes _geradorPdf;

    public ExportarTransacoesPdfUseCase(
        ITransacaoRepository transacaoRepository,
        IMembroRepository membroRepository,
        IContaRepository contaRepository,
        IContaConjuntaRepository contaConjuntaRepository,
        IGeradorPdfTransacoes geradorPdf)
    {
        _transacaoRepository = transacaoRepository ?? throw new ArgumentNullException(nameof(transacaoRepository));
        _membroRepository = membroRepository ?? throw new ArgumentNullException(nameof(membroRepository));
        _contaRepository = contaRepository ?? throw new ArgumentNullException(nameof(contaRepository));
        _contaConjuntaRepository = contaConjuntaRepository ?? throw new ArgumentNullException(nameof(contaConjuntaRepository));
        _geradorPdf = geradorPdf ?? throw new ArgumentNullException(nameof(geradorPdf));
    }

    public async Task<RelatorioPdfResponse> ExecutarAsync(
        ExportarTransacoesPdfRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // 1. Validação dos filtros
        if (request.ContaId.HasValue && request.ContaConjuntaId.HasValue)
            throw new ArgumentException("Informe no máximo uma conta: ContaId OU ContaConjuntaId.");
        if (request.DataInicio.HasValue && request.DataFim.HasValue && request.DataInicio > request.DataFim)
            throw new ArgumentException("A data inicial não pode ser depois da data final.");

        // 2. Conta filtrada: precisa ser do membro (ou conjunta da qual ele participa)
        string nomeConta = "Todas as contas";
        if (request.ContaId is int contaId)
        {
            var conta = await _contaRepository.ObterPorIdAsync(contaId, cancellationToken)
                ?? throw new KeyNotFoundException("Conta não encontrada.");
            if (conta.MembroId != request.MembroId)
                throw new AcessoNegadoException("Você não tem permissão para acessar esta conta.");
            nomeConta = conta.Nome;
        }
        else if (request.ContaConjuntaId is int contaConjuntaId)
        {
            var contaConjunta = await _contaConjuntaRepository.ObterPorIdAsync(contaConjuntaId, cancellationToken)
                ?? throw new KeyNotFoundException("Conta conjunta não encontrada.");
            if (!contaConjunta.Membros.Any(m => m.Id == request.MembroId))
                throw new AcessoNegadoException("Você não tem permissão para acessar esta conta conjunta.");
            nomeConta = contaConjunta.Nome;
        }

        var membro = await _membroRepository.ObterPorIdAsync(request.MembroId, cancellationToken)
            ?? throw new KeyNotFoundException("Membro não encontrado.");

        // 3. Transações das contas do membro no período, com os demais filtros
        var transacoes = await _transacaoRepository.ObterParaRelatorioAsync(
            request.MembroId,
            idCategoria: null,
            idSubCategoria: null,
            apenasSemSubcategoria: false,
            dataInicio: request.DataInicio,
            dataFim: request.DataFim,
            ct: cancellationToken);

        var categoria = string.IsNullOrWhiteSpace(request.Categoria) ? null : request.Categoria.Trim();
        // Mesma ordem da tabela da tela (setas dos títulos)
        var ordenacao = OrdenacaoDeTransacoes.Ler(request.Ordenacao);
        var filtradas = transacoes
            .Where(t => request.Tipo is null || t.Tipo == request.Tipo)
            .Where(t => request.ContaId is null || t.Conta?.Id == request.ContaId)
            .Where(t => request.ContaConjuntaId is null || t.ContaConjunta?.Id == request.ContaConjuntaId)
            .Where(t => categoria is null || t.Categoria?.Nome == categoria);
        var itens = OrdenacaoDeTransacoes.Ordenar(filtradas, ordenacao)
            .Select(t => new TransacaoPdfItem
            {
                DataTransacao = t.DataTransacao,
                Descricao = string.IsNullOrWhiteSpace(t.Descricao) ? "Sem descrição" : t.Descricao,
                Categoria = t.Categoria?.Nome ?? "Sem categoria",
                Conta = t.Conta?.Nome ?? t.ContaConjunta?.Nome ?? "—",
                Tipo = t.Tipo,
                Status = t.Status,
                Valor = t.Valor,
            })
            .ToList();

        // 4. Filtros descritos para o cabeçalho do PDF
        var filtros = new List<(string, string)>
        {
            ("Período", DescreverPeriodo(request)),
            ("Tipo", request.Tipo switch
            {
                Transacao.TipoTransacao.ENTRADA => "Entradas",
                Transacao.TipoTransacao.SAIDA => "Saídas",
                _ => "Todas",
            }),
            ("Conta", nomeConta),
            ("Categoria", categoria ?? "Todas as categorias"),
            ("Ordem", OrdenacaoDeTransacoes.Descrever(ordenacao)),
        };

        var pdf = _geradorPdf.Gerar(new TransacoesPdfDados
        {
            NomeMembro = membro.Nome,
            Filtros = filtros,
            Itens = itens,
        });

        return new RelatorioPdfResponse
        {
            Conteudo = pdf,
            NomeArquivo = $"transacoes-{DateTime.Today:yyyy-MM-dd}.pdf",
        };
    }

    // "Esta semana (05/10/2026 a 07/10/2026)", "desde 01/10/2026", "Todo o período"...
    private static string DescreverPeriodo(ExportarTransacoesPdfRequest r)
    {
        string Data(DateOnly d) => d.ToString("dd/MM/yyyy", PtBr);
        var intervalo = (r.DataInicio, r.DataFim) switch
        {
            (DateOnly de, DateOnly ate) => $"{Data(de)} a {Data(ate)}",
            (DateOnly de, null) => $"{Data(de)} a {Data(DateOnly.FromDateTime(DateTime.Today))}",
            (null, DateOnly ate) => $"até {Data(ate)}",
            _ => "todas as datas",
        };
        var rotulo = string.IsNullOrWhiteSpace(r.Periodo) ? null : r.Periodo.Trim();
        return rotulo is null ? intervalo : $"{rotulo} ({intervalo})";
    }
}
