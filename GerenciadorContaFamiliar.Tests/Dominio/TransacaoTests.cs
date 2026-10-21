using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Tests.Apoio;
using static GerenciadorContaFamiliar.Domain.Transacao;

namespace GerenciadorContaFamiliar.Tests.Dominio;

public class TransacaoTests
{
    private readonly Membro _membro = Fabrica.Membro();

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Valor_precisa_ser_positivo(decimal valor)
    {
        var conta = Fabrica.Conta(_membro);
        Assert.Throws<ArgumentException>(() => Fabrica.Transacao(_membro, conta, valor: valor));
    }

    [Fact]
    public void So_pendente_pode_ter_data_no_futuro()
    {
        var conta = Fabrica.Conta(_membro);
        var amanha = Fabrica.Hoje.AddDays(1);

        Assert.Throws<ArgumentException>(() => Fabrica.Transacao(_membro, conta, data: amanha));
        var pendente = Fabrica.Transacao(_membro, conta, data: amanha, status: StatusTransacao.PENDENTE);
        Assert.Equal(StatusTransacao.PENDENTE, pendente.Status);
    }

    [Fact]
    public void Pendente_com_data_passada_vira_atrasada()
    {
        var conta = Fabrica.Conta(_membro);

        var transacao = Fabrica.Transacao(_membro, conta, data: Fabrica.Hoje.AddDays(-3), status: StatusTransacao.PENDENTE);

        Assert.Equal(StatusTransacao.ATRASADA, transacao.Status);
    }

    [Fact]
    public void Precisa_de_exatamente_uma_conta()
    {
        var conta = Fabrica.Conta(_membro);
        var conjunta = new ContaConjunta("Casa", null, _membro.Id);

        Assert.Throws<ArgumentException>(() => Fabrica.Transacao(_membro));
        Assert.Throws<ArgumentException>(() => Fabrica.Transacao(_membro, conta, conjunta));
    }

    [Fact]
    public void Recorrente_exige_periodicidade_e_avulsa_nao_aceita()
    {
        var conta = Fabrica.Conta(_membro);

        Assert.Throws<ArgumentException>(() => new Transacao(10m, null, Fabrica.Hoje, TipoTransacao.SAIDA,
            ClassificacaoTransacao.FIXA, true, null, _membro, null, null, conta, null));
        Assert.Throws<ArgumentException>(() => new Transacao(10m, null, Fabrica.Hoje, TipoTransacao.SAIDA,
            ClassificacaoTransacao.FIXA, false, PeriodicidadeTransacao.MENSAL, _membro, null, null, conta, null));
    }

    [Fact]
    public void Pagar_uma_pendente_entra_no_saldo_e_voltar_para_pendente_sai()
    {
        var conta = Fabrica.Conta(_membro, saldoInicial: 500m);
        var transacao = Fabrica.Transacao(_membro, conta, valor: 120m, status: StatusTransacao.PENDENTE);
        transacao.AplicarEmConta();
        Assert.Equal(500m, conta.Saldo);

        transacao.AtualizarStatus(StatusTransacao.CONCLUIDA);
        Assert.Equal(380m, conta.Saldo);

        transacao.AtualizarStatus(StatusTransacao.PENDENTE);
        Assert.Equal(500m, conta.Saldo);
    }

    [Fact]
    public void Repetir_o_mesmo_status_nao_aplica_duas_vezes()
    {
        var conta = Fabrica.Conta(_membro, saldoInicial: 500m);
        var transacao = Fabrica.Transacao(_membro, conta, valor: 120m);
        transacao.AplicarEmConta();

        transacao.AtualizarStatus(StatusTransacao.CONCLUIDA);

        Assert.Equal(380m, conta.Saldo);
    }

    [Fact]
    public void Nao_pode_concluir_uma_pendente_com_data_no_futuro()
    {
        var conta = Fabrica.Conta(_membro, saldoInicial: 500m);
        var transacao = Fabrica.Transacao(_membro, conta, data: Fabrica.Hoje.AddDays(5), status: StatusTransacao.PENDENTE);

        Assert.Throws<ArgumentException>(() => transacao.AtualizarStatus(StatusTransacao.CONCLUIDA));
    }

    [Fact]
    public void Na_conta_conjunta_o_saldo_comeca_em_zero_sem_saldo_inicial()
    {
        var conjunta = new ContaConjunta("Casa", null, _membro.Id);

        Fabrica.Transacao(_membro, conjunta: conjunta, valor: 40m, tipo: TipoTransacao.ENTRADA).AplicarEmConta();

        Assert.Equal(40m, conjunta.Saldo);
    }
}
