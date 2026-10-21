using GerenciadorContaFamiliar.Application.DTOs.Requests.Membros;
using GerenciadorContaFamiliar.Application.UseCases.Membros;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Tests.Apoio;
using NSubstitute;

namespace GerenciadorContaFamiliar.Tests.CasosDeUso;

public class AlterarSenhaTests
{
    private readonly IMembroRepository _membros = Substitute.For<IMembroRepository>();
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly Membro _membro = Fabrica.MembroComSenha(1, "Antiga@1");

    public AlterarSenhaTests()
    {
        _membros.ObterPorIdAsync(1, Arg.Any<CancellationToken>()).Returns(_membro);
        _tokens.GerarToken(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<int>())
            .Returns(("token-novo", new DateTime(2026, 10, 14, 13, 0, 0, DateTimeKind.Utc)));
    }

    private AlterarSenhaUseCase CasoDeUso() => new(_membros, new HasherFalso(), _tokens, _unitOfWork);

    private static AlterarSenhaRequest Pedido(string atual, string nova, string? confirmacao = null)
        => new() { MembroId = 1, SenhaAtual = atual, NovaSenha = nova, ConfirmacaoSenha = confirmacao ?? nova };

    [Fact]
    public async Task Troca_a_senha_e_devolve_um_token_com_a_versao_nova()
    {
        var resposta = await CasoDeUso().ExecutarAsync(Pedido("Antiga@1", "Nova@123"));

        Assert.Equal("token-novo", resposta.Token);
        Assert.True(_membro.VerificarSenha("Nova@123", new HasherFalso()));
        _tokens.Received(1).GerarToken(1, _membro.Email, 1);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Senha_atual_errada_e_recusada()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => CasoDeUso().ExecutarAsync(Pedido("errada", "Nova@123")));

        Assert.True(_membro.VerificarSenha("Antiga@1", new HasherFalso()));
        Assert.Equal(0, _membro.VersaoSessao);
    }

    [Fact]
    public async Task Confirmacao_diferente_e_recusada()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            CasoDeUso().ExecutarAsync(Pedido("Antiga@1", "Nova@123", "Outra@123")));
    }
}

public class PreferenciasUsoCasosDeUsoTests
{
    private readonly IPreferenciaUsoRepository _preferencias = Substitute.For<IPreferenciaUsoRepository>();
    private readonly IContaRepository _contas = Substitute.For<IContaRepository>();
    private readonly IContaConjuntaRepository _conjuntas = Substitute.For<IContaConjuntaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private SalvarPreferenciasUsoUseCase Salvar() => new(_preferencias, _contas, _conjuntas, _unitOfWork);

    private static SalvarPreferenciasUsoRequest Pedido(string? conta, int periodo = 3)
        => new() { MembroId = 1, ContaPadrao = conta, PeriodoRelatorios = periodo, SemanaComecaNoDomingo = true };

    [Fact]
    public async Task Quem_nunca_salvou_recebe_os_padroes()
    {
        var resposta = await new ObterPreferenciasUsoUseCase(_preferencias).ExecutarAsync(1);

        Assert.Equal(1, resposta.PeriodoRelatorios);
        Assert.Null(resposta.ContaPadrao);
        Assert.False(resposta.SemanaComecaNoDomingo);
    }

    [Fact]
    public async Task Primeira_vez_cria_e_salva()
    {
        _contas.ObterPorIdAsync(10, Arg.Any<CancellationToken>()).Returns(Fabrica.Conta(Fabrica.Membro(1), id: 10));

        var resposta = await Salvar().ExecutarAsync(Pedido("c:10"));

        Assert.Equal("c:10", resposta.ContaPadrao);
        Assert.Equal(3, resposta.PeriodoRelatorios);
        await _preferencias.Received(1).AdicionarAsync(Arg.Is<PreferenciaUso>(p => p.MembroId == 1), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Conta_de_outro_membro_e_recusada()
    {
        _contas.ObterPorIdAsync(10, Arg.Any<CancellationToken>()).Returns(Fabrica.Conta(Fabrica.Membro(2), id: 10));

        await Assert.ThrowsAsync<ArgumentException>(() => Salvar().ExecutarAsync(Pedido("c:10")));
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Conta_inexistente_e_recusada()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Salvar().ExecutarAsync(Pedido("c:999")));
    }

    [Fact]
    public async Task Conjunta_so_vale_para_quem_participa()
    {
        var participante = Fabrica.Membro(1);
        var conjunta = new ContaConjunta("Casa", null, 5).ComId(7);
        _conjuntas.ObterPorIdAsync(7, Arg.Any<CancellationToken>()).Returns(conjunta);

        await Assert.ThrowsAsync<ArgumentException>(() => Salvar().ExecutarAsync(Pedido("cc:7")));

        conjunta.AdicionarMembro(participante);
        var resposta = await Salvar().ExecutarAsync(Pedido("cc:7"));
        Assert.Equal("cc:7", resposta.ContaPadrao);
    }

    [Fact]
    public async Task Sem_conta_padrao_nao_consulta_contas()
    {
        var existente = new PreferenciaUso(1);
        _preferencias.ObterPorMembroAsync(1, Arg.Any<CancellationToken>()).Returns(existente);

        var resposta = await Salvar().ExecutarAsync(Pedido(null, periodo: 12));

        Assert.Equal(12, resposta.PeriodoRelatorios);
        await _contas.DidNotReceive().ObterPorIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _preferencias.DidNotReceive().AdicionarAsync(Arg.Any<PreferenciaUso>(), Arg.Any<CancellationToken>());
    }
}
