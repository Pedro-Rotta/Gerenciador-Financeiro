using GerenciadorContaFamiliar.Application.DTOs.Requests.Membros;
using GerenciadorContaFamiliar.Application.UseCases.Membros;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Tests.Apoio;
using NSubstitute;
using static GerenciadorContaFamiliar.Domain.Transacao;

namespace GerenciadorContaFamiliar.Tests.CasosDeUso;

public class ExcluirMembroTests
{
    private const string Senha = "Teste@123";

    private readonly IMembroRepository _membros = Substitute.For<IMembroRepository>();
    private readonly IContaRepository _contas = Substitute.For<IContaRepository>();
    private readonly IContaConjuntaRepository _conjuntas = Substitute.For<IContaConjuntaRepository>();
    private readonly IRecorrenciaRepository _recorrencias = Substitute.For<IRecorrenciaRepository>();
    private readonly INotificacaoRepository _notificacoes = Substitute.For<INotificacaoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly Membro _membro = Fabrica.MembroComSenha(2, Senha);

    public ExcluirMembroTests()
    {
        _membros.ObterPorIdAsync(_membro.Id, Arg.Any<CancellationToken>()).Returns(_membro);
        SemConjuntas();
        _contas.ObterPorMembroAsync(_membro.Id, Arg.Any<CancellationToken>()).Returns(new List<Conta>());
    }

    private ExcluirMembroUseCase CasoDeUso() => new(
        _membros, _contas, _conjuntas, _recorrencias, _notificacoes, new HasherFalso(), _unitOfWork);

    private void SemConjuntas(params ContaConjunta[] conjuntas)
        => _conjuntas.ObterPorMembroAsync(_membro.Id, Arg.Any<CancellationToken>()).Returns(conjuntas.ToList());

    private Task Excluir(string senha = Senha)
        => CasoDeUso().ExecutarAsync(new ExcluirMembroRequest { MembroId = _membro.Id, Senha = senha });

    private Recorrencia Serie(Conta? conta = null, ContaConjunta? conjunta = null)
        => new(Fabrica.Transacao(_membro, conta, conjunta, data: new DateOnly(2026, 1, 5),
            periodicidade: PeriodicidadeTransacao.MENSAL), null);

    [Fact]
    public async Task Senha_errada_e_recusada_e_nada_muda()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Excluir("errada"));

        Assert.True(_membro.Ativo);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Inativa_e_derruba_as_sessoes()
    {
        await Excluir();

        Assert.False(_membro.Ativo);
        Assert.Equal(1, _membro.VersaoSessao);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Membro_ja_inativo_da_conflito()
    {
        _membro.Inativar();

        await Assert.ThrowsAsync<InvalidOperationException>(() => Excluir());
    }

    [Fact]
    public async Task Criador_de_conjunta_ativa_precisa_exclui_la_antes()
    {
        SemConjuntas(new ContaConjunta("Casa", null, _membro.Id).ComId(7));

        var erro = await Assert.ThrowsAsync<InvalidOperationException>(() => Excluir());

        Assert.Contains("\"Casa\"", erro.Message);
        Assert.True(_membro.Ativo);
    }

    [Fact]
    public async Task Participante_sai_da_conjunta_para_as_series_dele_e_o_criador_e_avisado()
    {
        var criador = Fabrica.Membro(1);
        var conjunta = new ContaConjunta("Casa", null, criador.Id).ComId(7);
        conjunta.AdicionarMembro(criador);
        conjunta.AdicionarMembro(_membro);
        SemConjuntas(conjunta);
        _conjuntas.ObterPorIdAsync(7, Arg.Any<CancellationToken>()).Returns(conjunta);
        var serie = Serie(conjunta: conjunta);
        _recorrencias.ObterAtivasPorContaConjuntaAsync(7, _membro.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Recorrencia> { serie });

        await Excluir();

        Assert.DoesNotContain(_membro, conjunta.Membros);
        Assert.False(serie.Ativa);
        await _notificacoes.Received(1).AdicionarAsync(
            Arg.Is<Notificacao>(n => n.Mensagem.Contains("saiu da conta conjunta")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Series_das_contas_proprias_param()
    {
        var conta = Fabrica.Conta(_membro, id: 20);
        _contas.ObterPorMembroAsync(_membro.Id, Arg.Any<CancellationToken>()).Returns(new List<Conta> { conta });
        var serie = Serie(conta);
        _recorrencias.ObterAtivasPorContaAsync(20, Arg.Any<CancellationToken>()).Returns(new List<Recorrencia> { serie });

        await Excluir();

        Assert.False(serie.Ativa);
    }
}
