namespace GerenciadorContaFamiliar.Domain.Shared.Interfaces;

/// <summary>
/// Limita a verificação ao abrir o app a no máximo uma vez por intervalo para cada membro.
/// </summary>
public interface ILimiteVerificacao
{
    /// <summary>
    /// Reserva a vez do membro: true se a verificação pode rodar agora (e marca o horário);
    /// false se já rodou dentro do intervalo.
    /// </summary>
    bool TentarReservar(int membroId);
}
