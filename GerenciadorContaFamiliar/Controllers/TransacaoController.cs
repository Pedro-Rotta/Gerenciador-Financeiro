using GerenciadorContaFamiliar.Application.DTOs.Requests.Recorrencias;
using GerenciadorContaFamiliar.Application.DTOs.Requests.Transacoes;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Recorrencias;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Transacoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GerenciadorContaFamiliar.Controllers;

[Authorize]
[Route("api/[controller]")]
public class TransacaoController : BaseApiController
{
    private readonly ICriarTransacaoUseCase _criarTransacaoUseCase;
    private readonly IEditarTransacaoUseCase _editarTransacaoUseCase;
    private readonly IExcluirTransacaoUseCase _excluirTransacaoUseCase;
    private readonly IListarTransacaoUseCase _listarTransacaoUseCase;
    private readonly IExportarTransacoesPdfUseCase _exportarTransacoesPdfUseCase;
    private readonly IPararRecorrenciaUseCase _pararRecorrenciaUseCase;

    public TransacaoController(
        ICriarTransacaoUseCase criarTransacaoUseCase,
        IEditarTransacaoUseCase editarTransacaoUseCase,
        IExcluirTransacaoUseCase excluirTransacaoUseCase,
        IListarTransacaoUseCase listarTransacaoUseCase,
        IExportarTransacoesPdfUseCase exportarTransacoesPdfUseCase,
        IPararRecorrenciaUseCase pararRecorrenciaUseCase)
    {
        _criarTransacaoUseCase = criarTransacaoUseCase ?? throw new ArgumentNullException(nameof(criarTransacaoUseCase));
        _editarTransacaoUseCase = editarTransacaoUseCase ?? throw new ArgumentNullException(nameof(editarTransacaoUseCase));
        _excluirTransacaoUseCase = excluirTransacaoUseCase ?? throw new ArgumentNullException(nameof(excluirTransacaoUseCase));
        _listarTransacaoUseCase = listarTransacaoUseCase ?? throw new ArgumentNullException(nameof(listarTransacaoUseCase));
        _exportarTransacoesPdfUseCase = exportarTransacoesPdfUseCase ?? throw new ArgumentNullException(nameof(exportarTransacoesPdfUseCase));
        _pararRecorrenciaUseCase = pararRecorrenciaUseCase ?? throw new ArgumentNullException(nameof(pararRecorrenciaUseCase));
    }
    [HttpPost]
    public async Task<ActionResult<TransacaoResponse>> Criar(
        [FromBody] CriarTransacaoRequest request,
        CancellationToken ct = default)
    {
        request.MembroId = ObterMembroIdAtual();
        var response = await _criarTransacaoUseCase.ExecutarAsync(request, ct);
        return CreatedAtAction(nameof(Criar), new { id = response.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TransacaoResponse>> Editar(
        int id,
        [FromBody] EditarTransacaoRequest request,
        CancellationToken ct = default)
    {
        request.Id = id;
        request.MembroId = ObterMembroIdAtual();
        var response = await _editarTransacaoUseCase.ExecutarAsync(request, ct);
        return Ok(response);
    }

    /// <summary>
    /// Exclui a transação. Em ocorrência de série, escopo=0 (só esta, padrão) ou
    /// escopo=1 (esta e as seguintes não concluídas; a série para de repetir).
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<TransacaoResponse>> Excluir(
        int id,
        [FromQuery] ExcluirTransacaoRequest.EscopoExclusao escopo = ExcluirTransacaoRequest.EscopoExclusao.ESTA,
        CancellationToken ct = default)
    {
        var response = await _excluirTransacaoUseCase.ExecutarAsync(
            new ExcluirTransacaoRequest { Id = id, MembroId = ObterMembroIdAtual(), Escopo = escopo }, ct);
        return Ok(response);
    }

    /// <summary>Para de repetir a série da ocorrência; as já geradas ficam como estão.</summary>
    [HttpPost("{id:int}/parar-recorrencia")]
    public async Task<ActionResult<TransacaoResponse>> PararRecorrencia(
        int id,
        CancellationToken ct = default)
    {
        var response = await _pararRecorrenciaUseCase.ExecutarAsync(
            new PararRecorrenciaRequest { TransacaoId = id, MembroId = ObterMembroIdAtual() }, ct);
        return Ok(response);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TransacaoResponse>>> Listar(
        [FromQuery] ListarTransacaoRequest request,
        CancellationToken ct = default)
    {
        // O controller injeta o MembroId do JWT (nunca confia no cliente).
        request.MembroId = ObterMembroIdAtual();

        var response = await _listarTransacaoUseCase.ExecutarAsync(request, ct);
        return Ok(response);
    }

    /// <summary>
    /// PDF das transações com os filtros da tabela do Dashboard (todos opcionais). Ex.:
    /// /api/Transacao/pdf?Tipo=1&amp;DataInicio=2026-10-01&amp;ContaId=2&amp;Categoria=Moradia&amp;Periodo=Este%20mês
    /// </summary>
    [HttpGet("pdf")]
    public async Task<IActionResult> ExportarPdf(
        [FromQuery] ExportarTransacoesPdfRequest request,
        CancellationToken ct = default)
    {
        request.MembroId = ObterMembroIdAtual();
        var pdf = await _exportarTransacoesPdfUseCase.ExecutarAsync(request, ct);
        return File(pdf.Conteudo, "application/pdf", pdf.NomeArquivo);
    }
}
