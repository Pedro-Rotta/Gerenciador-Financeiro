# Notificações — Parte 1 (avisos na hora da ação)

**Resumo:** as notificações ganharam tipo (define o ícone) e gravidade (define a cor:
verde, amarelo, vermelho) e passaram a avisar o que acontece nas contas conjuntas, quando o
saldo fica negativo, quando um lançamento fica sem categoria e o progresso das metas.

**Situação:** implementada e testada (48 verificações de ponta a ponta), **sem commit** ·
**Depende de:** — · **Usada por:** Transações recorrentes, Parte 2 ·
Índice: [roteiro](roteiro-notificacoes-e-recorrencias.md)

Legenda: **✔ você decidiu** · **⚑ proposta minha, para você confirmar**.

## Escopo

**Entrou**
- Tipo e gravidade nas notificações; ícone colorido no sino e em "Ver todas".
- Avisos de conta conjunta, saldo negativo, lançamento sem categoria e progresso de metas.
- Cor nas 4 notificações que já existiam (convites e participantes).

**Não entrou** (vai para outras especificações)
- Interruptores por usuário, saldo baixo, gasto acima do normal, vencimento, atrasada,
  prazo de meta → [Parte 2](notificacoes-parte2.md).
- Contas a pagar futuras → [Transações recorrentes](transacoes-recorrentes.md).

## Decisões

| Decisão | Escolha |
|---|---|
| Lista de avisos (conta conjunta, saldo, sem categoria, metas) | ✔ |
| Cor só no ícone, nunca na linha inteira | ✔ |
| Gravidade → cor: `INFORMATIVA` verde, `ATENCAO` amarelo (`ambar` no CSS), `CRITICA` vermelho | ✔ |
| Tipo → ícone: `GERAL` sino, `CONTA_CONJUNTA` usuários, `TRANSACAO` transações, `SALDO` carteira, `META` meta | ⚑ (implementado) |
| Notificações antigas ficam `GERAL` + `INFORMATIVA` (sino verde, igual a antes) | ⚑ (implementado) |
| Avisos de conta conjunta vão para os **outros** participantes, nunca para quem agiu | ⚑ (implementado) |
| Saldo negativo avisa **todos** os participantes, inclusive quem lançou | ⚑ (implementado) |
| Enums aninhados na entidade e trafegados como número (padrão do projeto) | padrão existente |

## Peças em comum

| Peça | Papel aqui |
|---|---|
| **C1 Tipo/gravidade + `AvisosNotificacao`** | **Criada aqui.** `AvisosNotificacao` avisa os outros participantes de uma conta conjunta e padroniza textos ("saída \"Mercado\" de R$ 120,50") e valores em R$ |

**O que as próximas especificações mudam no código desta parte**
- [Recorrentes](transacoes-recorrentes.md) (C4): os avisos que hoje estão dentro do
  `CriarTransacao` (lançamento de outro participante, saldo negativo, sem categoria) passam
  para `AvisosDeLancamento`, sem mudar o comportamento.
- [Parte 2](notificacoes-parte2.md): "lançamentos de outros participantes" e "progresso da
  meta" passam a respeitar o interruptor de quem recebe.

## Dados

- `Notificacao` ganhou `Tipo` (`GERAL`, `CONTA_CONJUNTA`, `TRANSACAO`, `SALDO`, `META`) e
  `Gravidade` (`INFORMATIVA`, `ATENCAO`, `CRITICA`), com padrão no construtor.
- Migração `AdicionarTipoEGravidadeEmNotificacao` (colunas `int`, padrão 0) — já aplicada no
  banco local.

## Avisos

| Aviso | Onde | Tipo | Gravidade | Quem recebe |
|---|---|---|---|---|
| Convite aceito | `AceitarConvite` | CONTA_CONJUNTA | INFORMATIVA | quem convidou |
| Convite recusado | `RecusarConvite` | CONTA_CONJUNTA | ATENCAO | quem convidou |
| Participante saiu | `SairContaConjunta` | CONTA_CONJUNTA | ATENCAO | criador |
| Você foi removido | `RemoverParticipante` | CONTA_CONJUNTA | ATENCAO | removido |
| Novo lançamento | `CriarTransacao` | CONTA_CONJUNTA | INFORMATIVA | outros participantes |
| Lançamento editado | `EditarTransacao` | CONTA_CONJUNTA | INFORMATIVA | outros participantes |
| Lançamento excluído | `ExcluirTransacao` | CONTA_CONJUNTA | ATENCAO | outros participantes |
| Categoria criada / alterada | `CriarCategoria` / `EditarCategoria` | CONTA_CONJUNTA | INFORMATIVA | outros participantes |
| Categoria excluída | `ExcluirCategoria` | CONTA_CONJUNTA | ATENCAO | outros participantes |
| Conta conjunta renomeada | `EditarContaConjunta` | CONTA_CONJUNTA | INFORMATIVA | outros participantes |
| Conta conjunta encerrada | `ExcluirContaConjunta` | CONTA_CONJUNTA | CRITICA | outros participantes |
| Saldo negativo | `CriarTransacao` | SALDO | CRITICA | dono / todos da conjunta |
| Lançamento sem categoria | `CriarTransacao` | TRANSACAO | ATENCAO | autor |
| Marco de meta (25, 50, 75%) | `VincularTransacaoMeta` | META | INFORMATIVA | dono da meta |
| Meta concluída | `VincularTransacaoMeta` | META | INFORMATIVA | dono da meta |

## Regras

- Só o autor edita/exclui a própria transação e só o criador edita/encerra a conta conjunta;
  "lançamento de outro" significa "de outro participante, na conta de todos".
- Edição que não muda nada (mesma descrição/status/categoria) não avisa; mudar só a descrição
  da conta conjunta também não.
- Saldo negativo avisa só na passagem de ≥ 0 para < 0; continuar negativo não repete.
- Aporte que pula vários marcos avisa só o maior; se conclui a meta, só a conclusão.
- O aviso é gravado junto com a ação, no mesmo salvamento.

## API

Sem rota nova. `GET api/Notificacao` passou a devolver `tipo` e `gravidade` (números).

## Tela

- `IconeNotificacao` (em `ModalNotificacoes.tsx`) desenha o ícone do tipo na cor da
  gravidade; usado no sino e em "Ver todas". Classes de cor que já existiam no CSS.

## Etapas (concluídas)

1. Base: tipo, gravidade, migração, ícone colorido, 4 notificações antigas classificadas.
2. Conta conjunta: `AvisosNotificacao` e os 7 avisos.
3. Saldo negativo, sem categoria, marcos e conclusão de meta.

## Verificação (feita)

`dotnet build` sem erros/avisos, `npx tsc -b`, `npx oxlint src`; testes de ponta a ponta
(6 + 26 + 16 verificações, todas passando); conferência visual das três cores; membros de
teste apagados.

## Riscos e pontos de atenção

- **Saldo negativo só ao criar:** `EditarTransacao` não muda valor; excluir uma entrada baixa
  o saldo mas não avisa.
- **Meta concluída ao editar:** `EditarMeta` → `AtualizarValorAlvo` não recalcula
  `Concluida`; baixar o alvo abaixo do valor atual não conclui a meta (bug antigo do domínio,
  fora deste escopo — aguardando sua decisão).
