namespace GerenciadorContaFamiliar.Application.Exceptions;

/// <summary>
/// O membro autenticado não tem permissão sobre o recurso (ex.: conta, transação
/// ou categoria de outro membro). Convertida em HTTP 403 pelo middleware de exceções.
/// </summary>
public class AcessoNegadoException : Exception
{
    public AcessoNegadoException(string mensagem) : base(mensagem) { }
}
