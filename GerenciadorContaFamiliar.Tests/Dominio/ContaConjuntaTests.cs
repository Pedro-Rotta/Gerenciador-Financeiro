using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Tests.Apoio;

namespace GerenciadorContaFamiliar.Tests.Dominio;

public class ContaConjuntaTests
{
    private readonly Membro _criador = Fabrica.Membro(1);
    private readonly Membro _participante = Fabrica.Membro(2);

    private ContaConjunta NovaConjunta()
    {
        var conjunta = new ContaConjunta("Casa", null, _criador.Id);
        conjunta.AdicionarMembro(_criador);
        conjunta.AdicionarMembro(_participante);
        return conjunta;
    }

    [Fact]
    public void Participante_pode_sair()
    {
        var conjunta = NovaConjunta();

        conjunta.RemoverMembro(_participante);

        Assert.DoesNotContain(_participante, conjunta.Membros);
    }

    [Fact]
    public void Criador_nao_sai_nem_e_removido()
    {
        var conjunta = NovaConjunta();

        Assert.Throws<InvalidOperationException>(() => conjunta.RemoverMembro(_criador));
        Assert.Contains(_criador, conjunta.Membros);
    }

    [Fact]
    public void Adicionar_o_mesmo_membro_duas_vezes_nao_duplica()
    {
        var conjunta = NovaConjunta();

        conjunta.AdicionarMembro(_participante);

        Assert.Equal(2, conjunta.Membros.Count);
    }

    [Fact]
    public void Conta_inativa_recusa_transacoes()
    {
        var conjunta = NovaConjunta();
        conjunta.Inativar();

        Assert.Throws<InvalidOperationException>(() =>
            Fabrica.Transacao(_participante, conjunta: conjunta).AplicarEmConta());
    }

    [Fact]
    public void Precisa_de_nome_e_criador()
    {
        Assert.Throws<ArgumentException>(() => new ContaConjunta(" ", null, 1));
        Assert.Throws<ArgumentException>(() => new ContaConjunta("Casa", null, 0));
    }
}
