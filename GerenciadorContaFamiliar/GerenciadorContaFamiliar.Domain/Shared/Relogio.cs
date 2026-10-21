namespace GerenciadorContaFamiliar.Domain.Shared;

/// <summary>
/// Data atual no fuso de Brasília. Usada em todas as regras de data: a data UTC já vira
/// "amanhã" a partir das 21h no Brasil.
/// </summary>
public static class Relogio
{
    private static readonly TimeZoneInfo FusoBrasilia = ObterFusoBrasilia();

    public static DateOnly HojeBrasilia() => DataBrasilia(DateTime.UtcNow);

    /// <summary>Data em Brasília de um instante UTC.</summary>
    public static DateOnly DataBrasilia(DateTime utc)
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, FusoBrasilia));

    // Nome IANA (Linux e Windows com ICU); o nome do Windows fica de reserva
    private static TimeZoneInfo ObterFusoBrasilia()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        }
    }
}
