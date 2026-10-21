using GerenciadorContaFamiliar.Domain.Shared;

namespace GerenciadorContaFamiliar.Domain;

/// <summary>
/// Código de 6 dígitos do "Esqueceu a senha", mandado por e-mail. Só o hash é guardado;
/// vale 15 minutos, aceita até 5 erros e é usado uma vez.
/// </summary>
public class CodigoRecuperacaoSenha : BaseEntity
{
    public const int MinutosDeValidade = 15;
    public const int TentativasMaximas = 5;

    // Limite por conta (protege o e-mail da pessoa e a cota de envio): 1 pedido por minuto e
    // no máximo 5 por hora; acima disso a resposta é a mesma, mas nenhum código é gerado
    public static readonly TimeSpan IntervaloEntrePedidos = TimeSpan.FromMinutes(1);
    public const int PedidosPorHora = 5;

    public int MembroId { get; private set; }
    public string CodigoHash { get; private set; }
    public DateTime ExpiraEm { get; private set; }
    public int Tentativas { get; private set; }
    public bool Usado { get; private set; }
    public DateTime CriadoEm { get; private set; }

    // Construtor privado usado pelo EF Core ao materializar entidades do banco.
#pragma warning disable CS8618
    private CodigoRecuperacaoSenha() { }
#pragma warning restore CS8618

    public CodigoRecuperacaoSenha(int membroId, string codigoHash, DateTime agoraUtc)
    {
        if (membroId <= 0)
            throw new ArgumentException("O membro é obrigatório.");
        if (string.IsNullOrWhiteSpace(codigoHash))
            throw new ArgumentException("O código é obrigatório.");
        MembroId = membroId;
        CodigoHash = codigoHash;
        CriadoEm = agoraUtc;
        ExpiraEm = agoraUtc.AddMinutes(MinutosDeValidade);
    }

    // Tentativas e uso de um código ativo mudam direto no banco (atômico, ver o repositório)
    public bool EstaValido(DateTime agoraUtc) => !Usado && Tentativas < TentativasMaximas && agoraUtc < ExpiraEm;
    public void MarcarUsado() => Usado = true;

    public override string ToString() => $"[{Id}] Código de recuperação do membro {MembroId}";
}
