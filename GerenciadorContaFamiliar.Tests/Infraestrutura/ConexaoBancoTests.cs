using GerenciadorContaFamiliar.Infrastructure.Data;
using Npgsql;

namespace GerenciadorContaFamiliar.Tests.Infraestrutura;

public class ConexaoBancoTests
{
    private const string Neon =
        "Host=ep-exemplo-123.us-east-1.aws.neon.tech; Database=neondb; Username=neondb_owner; " +
        "Password=senha-de-teste; SSL Mode=VerifyFull; Channel Binding=Require;";

    private static NpgsqlConnectionStringBuilder Ler(string cs) => new(cs);

    [Fact]
    public void Sem_tempo_definido_passa_a_esperar_30_segundos()
    {
        var resultado = Ler(ConexaoBanco.ComTempoMinimo(Neon));

        Assert.Equal(30, resultado.Timeout);
    }

    [Fact]
    public void Tempo_menor_que_o_minimo_sobe_para_30()
    {
        var resultado = Ler(ConexaoBanco.ComTempoMinimo("Host=localhost;Database=x;Username=u;Password=p;Timeout=10"));

        Assert.Equal(30, resultado.Timeout);
    }

    [Fact]
    public void Tempo_maior_que_o_minimo_e_mantido()
    {
        var resultado = Ler(ConexaoBanco.ComTempoMinimo("Host=localhost;Database=x;Username=u;Password=p;Timeout=60"));

        Assert.Equal(60, resultado.Timeout);
    }

    [Fact]
    public void Mantem_os_outros_campos_do_Neon()
    {
        var resultado = Ler(ConexaoBanco.ComTempoMinimo(Neon));

        Assert.Equal("ep-exemplo-123.us-east-1.aws.neon.tech", resultado.Host);
        Assert.Equal("neondb", resultado.Database);
        Assert.Equal("neondb_owner", resultado.Username);
        Assert.Equal("senha-de-teste", resultado.Password);
        Assert.Equal(SslMode.VerifyFull, resultado.SslMode);
    }

    [Theory]
    [InlineData("postgresql://neondb_owner:senha@ep-exemplo.us-east-1.aws.neon.tech/neondb?sslmode=require")]
    [InlineData("isto não é uma connection string")]
    public void Formato_errado_da_um_erro_claro(string cs)
    {
        var erro = Assert.Throws<InvalidOperationException>(() => ConexaoBanco.ComTempoMinimo(cs));

        Assert.Contains(".NET", erro.Message);
        Assert.DoesNotContain("senha", erro.Message);
    }
}
