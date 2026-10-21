using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Tests.Apoio;
using static GerenciadorContaFamiliar.Domain.Transacao;

namespace GerenciadorContaFamiliar.Tests.Dominio;

public class ContaTests
{
    private readonly Membro _membro = Fabrica.Membro();

    [Fact]
    public void Conta_nova_comeca_sem_saldo_inicial_e_recusa_transacoes()
    {
        var conta = Fabrica.Conta(_membro, saldoInicial: null);
        var transacao = Fabrica.Transacao(_membro, conta);

        var erro = Assert.Throws<InvalidOperationException>(() => conta.EfetuarTransacao(transacao));
        Assert.Contains("saldo inicial", erro.Message);
        Assert.False(conta.SaldoInicialDefinido);
        Assert.Equal(0m, conta.Saldo);
    }

    [Fact]
    public void Saldo_inicial_entra_no_saldo_e_so_pode_ser_informado_uma_vez()
    {
        var conta = Fabrica.Conta(_membro, saldoInicial: 250m);

        Assert.True(conta.SaldoInicialDefinido);
        Assert.Equal(250m, conta.SaldoInicial);
        Assert.Equal(250m, conta.Saldo);
        Assert.Throws<InvalidOperationException>(() => conta.DefinirSaldoInicial(10m));
    }

    [Theory]
    [InlineData(TipoTransacao.ENTRADA, 1100)]
    [InlineData(TipoTransacao.SAIDA, 900)]
    public void Transacao_concluida_mexe_no_saldo(TipoTransacao tipo, decimal saldoEsperado)
    {
        var conta = Fabrica.Conta(_membro, saldoInicial: 1000m);

        conta.EfetuarTransacao(Fabrica.Transacao(_membro, conta, tipo: tipo));

        Assert.Equal(saldoEsperado, conta.Saldo);
        Assert.Single(conta.Transacoes);
    }

    [Fact]
    public void Transacao_pendente_nao_mexe_no_saldo()
    {
        var conta = Fabrica.Conta(_membro, saldoInicial: 1000m);

        conta.EfetuarTransacao(Fabrica.Transacao(_membro, conta, status: StatusTransacao.PENDENTE));

        Assert.Equal(1000m, conta.Saldo);
        Assert.Single(conta.Transacoes);
    }

    [Fact]
    public void Estornar_desfaz_so_a_concluida()
    {
        var conta = Fabrica.Conta(_membro, saldoInicial: 1000m);
        var paga = Fabrica.Transacao(_membro, conta, valor: 300m);
        var pendente = Fabrica.Transacao(_membro, conta, valor: 50m, status: StatusTransacao.PENDENTE);
        conta.EfetuarTransacao(paga);
        conta.EfetuarTransacao(pendente);

        conta.EstornarTransacao(paga);
        conta.EstornarTransacao(pendente);

        Assert.Equal(1000m, conta.Saldo);
    }

    [Fact]
    public void Recusa_transacao_de_outra_conta()
    {
        var conta = Fabrica.Conta(_membro, saldoInicial: 0m, id: 10);
        var outra = Fabrica.Conta(_membro, saldoInicial: 0m, id: 11);

        Assert.Throws<InvalidOperationException>(() => conta.EfetuarTransacao(Fabrica.Transacao(_membro, outra)));
    }

    [Fact]
    public void Conta_inativa_recusa_transacoes()
    {
        var conta = Fabrica.Conta(_membro, saldoInicial: 0m);
        conta.Inativar();

        Assert.Throws<InvalidOperationException>(() => conta.EfetuarTransacao(Fabrica.Transacao(_membro, conta)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Nome_e_obrigatorio(string nome)
    {
        Assert.Throws<ArgumentException>(() => new Conta(nome, 1));
    }
}
