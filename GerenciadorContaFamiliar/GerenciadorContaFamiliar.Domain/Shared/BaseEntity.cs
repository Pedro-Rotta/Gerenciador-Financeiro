namespace GerenciadorContaFamiliar.Domain.Shared;
public abstract class BaseEntity
{
    public int Id { get; protected set; }
    public abstract override string ToString();

}