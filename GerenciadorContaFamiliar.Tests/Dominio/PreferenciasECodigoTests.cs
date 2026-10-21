using GerenciadorContaFamiliar.Domain;

namespace GerenciadorContaFamiliar.Tests.Dominio;

public class PreferenciaUsoTests
{
    [Fact]
    public void Padroes_semana_na_segunda_conta_principal_um_mes_valores_a_mostra()
    {
        var preferencia = new PreferenciaUso(1);

        Assert.False(preferencia.SemanaComecaNoDomingo);
        Assert.Null(preferencia.ContaPadrao);
        Assert.Equal(1, preferencia.PeriodoRelatorios);
        Assert.False(preferencia.OcultarValores);
    }

    [Theory]
    [InlineData("c:12", "c:12")]
    [InlineData(" cc:3 ", "cc:3")]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Conta_padrao_aceita_os_dois_formatos_ou_vazio(string? entrada, string? esperado)
    {
        var preferencia = new PreferenciaUso(1);

        preferencia.Atualizar(true, entrada, 6, true);

        Assert.Equal(esperado, preferencia.ContaPadrao);
        Assert.Equal(6, preferencia.PeriodoRelatorios);
        Assert.True(preferencia.SemanaComecaNoDomingo);
        Assert.True(preferencia.OcultarValores);
    }

    [Theory]
    [InlineData("x:1")]
    [InlineData("c:")]
    [InlineData("c:abc")]
    public void Conta_padrao_em_formato_errado_e_recusada(string conta)
    {
        Assert.Throws<ArgumentException>(() => new PreferenciaUso(1).Atualizar(false, conta, 1, false));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(24)]
    public void Periodo_dos_relatorios_so_aceita_1_3_6_ou_12(int meses)
    {
        Assert.Throws<ArgumentException>(() => new PreferenciaUso(1).Atualizar(false, null, meses, false));
    }
}

public class CodigoRecuperacaoSenhaTests
{
    private static readonly DateTime Agora = new(2026, 10, 14, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Vale_por_15_minutos()
    {
        var codigo = new CodigoRecuperacaoSenha(1, "hash", Agora);

        Assert.True(codigo.EstaValido(Agora.AddMinutes(14)));
        Assert.False(codigo.EstaValido(Agora.AddMinutes(15)));
    }

    [Fact]
    public void Codigo_usado_nao_vale_mais()
    {
        var codigo = new CodigoRecuperacaoSenha(1, "hash", Agora);

        codigo.MarcarUsado();

        Assert.False(codigo.EstaValido(Agora));
    }

    [Fact]
    public void Precisa_de_membro_e_codigo()
    {
        Assert.Throws<ArgumentException>(() => new CodigoRecuperacaoSenha(0, "hash", Agora));
        Assert.Throws<ArgumentException>(() => new CodigoRecuperacaoSenha(1, " ", Agora));
    }
}
