using GerenciadorContaFamiliar.Domain.Shared;

namespace GerenciadorContaFamiliar.Domain;

public class LogAuditoria : BaseEntity
{
    public enum OperacaoLog {INSERT, UPDATE, DELETE}

    public string Tabela { get; private set; }
    public int IdRegistro { get; private set; }
    public OperacaoLog Operacao { get; private set; }
    public string? DadosAntigos { get; private set; }
    public string? DadosNovos { get; private set; }
    public int? IdMembro { get; private set; }
    public DateTime CriadoEm { get; private set; }

    // Construtor privado usado pelo EF Core ao materializar entidades do banco.
#pragma warning disable CS8618
    private LogAuditoria() { }
#pragma warning restore CS8618

    public LogAuditoria(string tabela, int idRegistro, OperacaoLog operacao, string? dadosAntigos, string? dadosNovos, int? idMembro)
    {
        
        if(string.IsNullOrWhiteSpace(tabela))
            throw new ArgumentException("O nome da tabela nao pode ser nulo");
        if(idRegistro <= 0)
            throw new ArgumentException("O id do registro tem que ser valido");
        if (!Enum.IsDefined(typeof(OperacaoLog), operacao))
            throw new ArgumentException("Operação de log inválida.");
        if (idMembro.HasValue && idMembro.Value <= 0)
            throw new ArgumentException("O id do membro tem que ser valido");

        Tabela = tabela;
        IdRegistro = idRegistro;
        Operacao = operacao;
        DadosAntigos = dadosAntigos;
        DadosNovos = dadosNovos;
        IdMembro = idMembro;
        CriadoEm = DateTime.UtcNow;
    }

    public override string ToString() => $"[{Id}] {Operacao}";
}