using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Tests.Apoio;
using static GerenciadorContaFamiliar.Domain.Transacao;

namespace GerenciadorContaFamiliar.Tests.Dominio;

public class RecorrenciaTests
{
    private readonly Membro _membro = Fabrica.Membro();

    private Recorrencia NovaSerie(DateOnly inicio, PeriodicidadeTransacao periodicidade, int? total = null)
    {
        var conta = Fabrica.Conta(_membro);
        var primeira = Fabrica.Transacao(_membro, conta, data: inicio, periodicidade: periodicidade);
        return new Recorrencia(primeira, total);
    }

    [Fact]
    public void Mensal_no_dia_31_usa_o_ultimo_dia_do_mes_e_volta_ao_dia_original()
    {
        var serie = NovaSerie(new DateOnly(2026, 1, 31), PeriodicidadeTransacao.MENSAL);

        var proximas = serie.ProximasOcorrencias(new DateOnly(2026, 4, 30));

        Assert.Equal(
            new[] { new DateOnly(2026, 2, 28), new DateOnly(2026, 3, 31), new DateOnly(2026, 4, 30) },
            proximas.Select(p => p.Data));
        Assert.Equal(new[] { 2, 3, 4 }, proximas.Select(p => p.Numero));
    }

    [Fact]
    public void Semanal_soma_sete_dias()
    {
        var serie = NovaSerie(new DateOnly(2026, 9, 1), PeriodicidadeTransacao.SEMANAL);

        Assert.Equal(new DateOnly(2026, 9, 8), serie.ProximaData);
    }

    [Fact]
    public void Com_quantidade_definida_para_ao_atingir_o_total()
    {
        var serie = NovaSerie(new DateOnly(2026, 1, 10), PeriodicidadeTransacao.MENSAL, total: 3);

        Assert.Equal(2, serie.RegistrarOcorrencia());
        Assert.True(serie.Ativa);
        Assert.Equal(3, serie.RegistrarOcorrencia());

        Assert.False(serie.Ativa);
        Assert.True(serie.AtingiuTotal);
        Assert.Throws<InvalidOperationException>(() => serie.RegistrarOcorrencia());
    }

    [Fact]
    public void Previsao_respeita_o_total()
    {
        var serie = NovaSerie(new DateOnly(2026, 1, 10), PeriodicidadeTransacao.MENSAL, total: 2);

        var proximas = serie.ProximasOcorrencias(new DateOnly(2027, 1, 1));

        Assert.Single(proximas);
    }

    [Fact]
    public void Serie_parada_nao_preve_nem_gera()
    {
        var serie = NovaSerie(new DateOnly(2026, 1, 10), PeriodicidadeTransacao.MENSAL);

        serie.Parar();

        Assert.Empty(serie.ProximasOcorrencias(new DateOnly(2027, 1, 1)));
        Assert.Throws<InvalidOperationException>(() => serie.RegistrarOcorrencia());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(121)]
    public void Quantidade_fora_do_limite_e_recusada(int total)
    {
        Assert.Throws<ArgumentException>(() =>
            NovaSerie(new DateOnly(2026, 1, 10), PeriodicidadeTransacao.MENSAL, total));
    }

    [Fact]
    public void Transacao_avulsa_nao_vira_serie()
    {
        var conta = Fabrica.Conta(_membro);

        Assert.Throws<ArgumentException>(() => new Recorrencia(Fabrica.Transacao(_membro, conta), null));
    }
}
