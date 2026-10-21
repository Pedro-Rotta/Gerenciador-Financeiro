using Npgsql;

namespace GerenciadorContaFamiliar.Infrastructure.Data;

/// <summary>
/// Ajustes na connection string do PostgreSQL antes de entregá-la ao EF Core.
/// </summary>
public static class ConexaoBanco
{
    /// <summary>
    /// Espera mínima (segundos) para abrir a conexão: o Neon gratuito dorme depois de 5 minutos
    /// parado e a primeira conexão ao acordar é mais lenta.
    /// </summary>
    public const int TempoMinimoConexaoSegundos = 30;

    /// <summary>
    /// Devolve a connection string com Timeout de pelo menos 30 s. Formato inválido (por exemplo, o
    /// "postgresql://..." do Neon) vira um erro que diz o que fazer, sem repetir a string (tem a senha).
    /// </summary>
    public static string ComTempoMinimo(string connectionString)
    {
        NpgsqlConnectionStringBuilder builder;
        try
        {
            builder = new NpgsqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection não está no formato do .NET " +
                "(Host=...;Database=...;Username=...;Password=...). No painel do Neon, em Connect, " +
                "escolha a opção \".NET\" e cole de novo.");
        }

        if (builder.Timeout < TempoMinimoConexaoSegundos)
            builder.Timeout = TempoMinimoConexaoSegundos;
        return builder.ConnectionString;
    }
}
