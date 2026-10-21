using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Tests.Apoio;

namespace GerenciadorContaFamiliar.Tests.Dominio;

public class MembroTests
{
    private readonly HasherFalso _hasher = new();

    [Fact]
    public void Senha_e_conferida_pelo_hash()
    {
        var membro = Fabrica.MembroComSenha(1, "Segredo@1");

        Assert.True(membro.VerificarSenha("Segredo@1", _hasher));
        Assert.False(membro.VerificarSenha("errada", _hasher));
    }

    [Fact]
    public void Trocar_a_senha_derruba_as_sessoes()
    {
        var membro = Fabrica.MembroComSenha(1, "Segredo@1");
        var versao = membro.VersaoSessao;

        membro.TrocarSenha("NovaSenha@2", _hasher);

        Assert.Equal(versao + 1, membro.VersaoSessao);
        Assert.True(membro.VerificarSenha("NovaSenha@2", _hasher));
        Assert.False(membro.VerificarSenha("Segredo@1", _hasher));
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("")]
    public void Senha_nova_curta_e_recusada(string senha)
    {
        var membro = Fabrica.MembroComSenha(1, "Segredo@1");

        Assert.Throws<ArgumentException>(() => membro.TrocarSenha(senha, _hasher));
        Assert.Equal(0, membro.VersaoSessao);
    }

    [Fact]
    public void Encerrar_sessoes_sobe_a_versao()
    {
        var membro = Fabrica.Membro();

        membro.EncerrarSessoes();
        membro.EncerrarSessoes();

        Assert.Equal(2, membro.VersaoSessao);
    }

    [Fact]
    public void Membro_novo_e_ativo_e_pode_ser_inativado()
    {
        var membro = Fabrica.Membro();
        Assert.True(membro.Ativo);

        membro.Inativar();

        Assert.False(membro.Ativo);
    }

    [Fact]
    public void Nome_usuario_e_email_sao_obrigatorios()
    {
        Assert.Throws<ArgumentException>(() => new Membro(" ", "u", "e@x", null));
        Assert.Throws<ArgumentException>(() => new Membro("N", " ", "e@x", null));
        Assert.Throws<ArgumentException>(() => new Membro("N", "u", " ", null));
    }
}
