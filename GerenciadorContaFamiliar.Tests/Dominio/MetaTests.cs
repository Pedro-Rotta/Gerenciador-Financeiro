using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Tests.Apoio;

namespace GerenciadorContaFamiliar.Tests.Dominio;

public class MetaTests
{
    private static Meta NovaMeta(decimal alvo = 1000m) => new("Viagem", alvo, Fabrica.Hoje.AddDays(30), 1);

    [Fact]
    public void Meta_nova_comeca_zerada_e_em_andamento()
    {
        var meta = NovaMeta();

        Assert.Equal(0m, meta.ValorAtual);
        Assert.False(meta.Concluida);
        Assert.True(meta.Ativo);
    }

    [Fact]
    public void Conclui_ao_atingir_o_alvo()
    {
        var meta = NovaMeta(1000m);

        meta.RegistrarProgresso(400m);
        Assert.False(meta.Concluida);

        meta.RegistrarProgresso(600m);
        Assert.True(meta.Concluida);
        Assert.Equal(1000m, meta.ValorAtual);
    }

    [Fact]
    public void Aumentar_o_alvo_reabre_a_meta()
    {
        var meta = NovaMeta(100m);
        meta.RegistrarProgresso(100m);

        meta.AtualizarValorAlvo(150m);

        Assert.False(meta.Concluida);
    }

    [Fact]
    public void Valor_atual_recalculado_atualiza_a_conclusao()
    {
        var meta = NovaMeta(100m);
        meta.AtualizarValorAtual(120m);
        Assert.True(meta.Concluida);

        meta.AtualizarValorAtual(30m);
        Assert.False(meta.Concluida);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Progresso_e_alvo_precisam_ser_positivos(decimal valor)
    {
        var meta = NovaMeta();

        Assert.Throws<ArgumentException>(() => meta.RegistrarProgresso(valor));
        Assert.Throws<ArgumentException>(() => meta.AtualizarValorAlvo(valor));
        Assert.Throws<ArgumentException>(() => new Meta("X", valor, Fabrica.Hoje.AddDays(1), 1));
    }

    [Fact]
    public void Prazo_no_passado_e_recusado()
    {
        Assert.Throws<ArgumentException>(() => new Meta("X", 10m, Fabrica.Hoje.AddDays(-2), 1));
        Assert.Throws<ArgumentException>(() => NovaMeta().AtualizarPrazo(Fabrica.Hoje.AddDays(-2)));
    }
}
