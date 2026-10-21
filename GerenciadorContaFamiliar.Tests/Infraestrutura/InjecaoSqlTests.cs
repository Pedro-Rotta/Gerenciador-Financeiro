using GerenciadorContaFamiliar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorContaFamiliar.Tests.Infraestrutura;

/// <summary>
/// O banco só é consultado pelo LINQ do EF Core (nenhum FromSqlRaw/ExecuteSqlRaw): o texto digitado
/// vai como parâmetro (@__...), nunca colado no SQL. Estes testes geram o SQL sem abrir conexão.
/// </summary>
public class InjecaoSqlTests
{
    public static TheoryData<string> Ataques => new()
    {
        "admin' OR '1'='1",
        "' OR 1=1--",
        "x'; DROP TABLE \"Membros\";--",
        "' UNION SELECT \"SenhaHash\" FROM \"Membros\"--",
        "'; SELECT pg_sleep(5);--",
    };

    private static GerenciadorContaFamiliarDbContext Contexto() =>
        new(new DbContextOptionsBuilder<GerenciadorContaFamiliarDbContext>()
            .UseNpgsql("Host=localhost;Database=x;Username=u;Password=p")
            .Options);

    // ToQueryString põe a declaração dos parâmetros em comentários "-- @__x='valor'" no topo;
    // o SQL de verdade é o resto
    private static string SomenteOSql(string consulta) =>
        string.Join('\n', consulta.Split('\n').Where(linha => !linha.TrimStart().StartsWith("--")));

    [Theory]
    [MemberData(nameof(Ataques))]
    public void Login_por_nome_de_usuario_manda_o_texto_como_parametro(string ataque)
    {
        using var contexto = Contexto();

        var sql = SomenteOSql(contexto.Membros.Where(m => m.NomeDeUsuario == ataque).ToQueryString());

        Assert.Contains("@__ataque_0", sql);
        Assert.DoesNotContain(ataque, sql);
    }

    [Theory]
    [MemberData(nameof(Ataques))]
    public void Busca_por_email_manda_o_texto_como_parametro(string ataque)
    {
        using var contexto = Contexto();
        var email = ataque.ToLower();

        var sql = SomenteOSql(contexto.Membros.Where(m => m.Email.ToLower() == email).ToQueryString());

        Assert.Contains("@__email_0", sql);
        Assert.DoesNotContain(email, sql);
    }
}
