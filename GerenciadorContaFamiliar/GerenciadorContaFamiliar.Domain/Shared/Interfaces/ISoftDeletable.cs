namespace GerenciadorContaFamiliar.Domain.Shared.Interfaces;
public interface ISoftDeletable
{
    bool Ativo { get; }
    void Ativar();
    void Inativar();
}
