using System.Reflection;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using static GerenciadorContaFamiliar.Domain.Transacao;

namespace GerenciadorContaFamiliar.Tests.Apoio;

/// <summary>Monta entidades válidas para os testes (o Id normalmente vem do banco).</summary>
internal static class Fabrica
{
    public static DateOnly Hoje => Relogio.HojeBrasilia();

    /// <summary>Define o Id de uma entidade (setter protegido, preenchido pelo EF no app).</summary>
    public static T ComId<T>(this T entidade, int id) where T : BaseEntity
    {
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!
            .SetValue(entidade, id, BindingFlags.Instance | BindingFlags.NonPublic, null, null, null);
        return entidade;
    }

    public static Membro Membro(int id = 1, string usuario = "membro")
        => new Membro($"Membro {id}", $"{usuario}{id}", $"{usuario}{id}@teste.local", null).ComId(id);

    /// <summary>Membro com senha definida pelo hasher falso.</summary>
    public static Membro MembroComSenha(int id, string senha)
    {
        var membro = Membro(id);
        membro.DefinirSenha(senha, new HasherFalso());
        return membro;
    }

    public static Conta Conta(Membro dono, decimal? saldoInicial = 0m, int id = 10)
    {
        var conta = new Conta("Conta", dono.Id).ComId(id);
        if (saldoInicial.HasValue)
            conta.DefinirSaldoInicial(saldoInicial.Value);
        return conta;
    }

    public static Transacao Transacao(
        Membro membro, Conta? conta = null, ContaConjunta? conjunta = null,
        decimal valor = 100m, TipoTransacao tipo = TipoTransacao.SAIDA,
        StatusTransacao status = StatusTransacao.CONCLUIDA, DateOnly? data = null,
        PeriodicidadeTransacao? periodicidade = null)
        => new Transacao(valor, "teste", data ?? Hoje, tipo, ClassificacaoTransacao.VARIAVEL,
            periodicidade.HasValue, periodicidade, membro, null, null, conta, conjunta, status);
}

/// <summary>Hasher previsível: "h:" + senha.</summary>
internal sealed class HasherFalso : IPasswordHasher
{
    public string Hash(string senha) => "h:" + senha;
    public bool Verify(string senha, string hash) => hash == "h:" + senha;
}
