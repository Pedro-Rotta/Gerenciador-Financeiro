# Roteiro — Notificações e transações recorrentes

**Situação:** executado (Parte 1, Transações recorrentes e Parte 2 implementadas até 09/10/2026).
As decisões que ficaram abertas aqui (P1, R8 e N5) foram resolvidas em 13/10/2026 em
[saldo-metas-gasto.md](saldo-metas-gasto.md).

Índice das especificações, ordem de construção e o que elas têm em comum.
Comece por aqui; cada especificação tem o mesmo formato (resumo → escopo → decisões →
peças em comum → dados → regras → API → tela → etapas → verificação → riscos).

Legenda das decisões: **✔ você decidiu** · **⚑ proposta minha, para você confirmar**.

## Ordem de construção

| # | Especificação | Situação | Depende de |
|---|---|---|---|
| 1 | [Notificações — Parte 1](notificacoes-parte1.md): avisos na hora da ação, cores por gravidade | Implementada, testada e commitada | — |
| 2 | [Transações recorrentes](transacoes-recorrentes.md): séries mensais/semanais que se repetem sozinhas | Implementada e testada (09/10) | 1 |
| 3 | [Notificações — Parte 2](notificacoes-parte2.md): aba de Configurações funcionando, avisos de tempo | Implementada e testada (09/10) | 1 e 2 |

Por que recorrentes antes da Parte 2: as recorrências criam as contas a pagar futuras
(pendentes com data futura). Sem elas, o lembrete de vencimento e o aviso de atrasada da
Parte 2 só funcionariam para o que alguém lançasse à mão.

## Peças em comum

Cada peça é construída **uma vez**, na primeira especificação que precisa dela, e reaproveitada
pelas seguintes.

| Peça | O que é | Construída em | Usada por |
|---|---|---|---|
| **C1. Tipo e gravidade da notificação** | Ícone por tipo, cor por gravidade (verde/amarelo/vermelho); ajudante `AvisosNotificacao` (avisar participantes, textos, R$) | Parte 1 ✅ | Recorrentes, Parte 2 |
| **C2. "Hoje" em Brasília** | Data atual no fuso `America/Sao_Paulo`, usada em todas as regras de data (hoje o domínio usa a data UTC, que já é "amanhã" a partir das 21h) | Recorrentes | Recorrentes, Parte 2 |
| **C3. Data futura para PENDENTE** | Transação pendente pode ter data futura (conta a pagar); concluída/atrasada continua limitada a hoje | Recorrentes | Recorrentes (ocorrências futuras), Parte 2 (lembrete de vencimento) |
| **C4. Avisos ao lançar** | Um único ponto que roda os avisos de "transação criada" (saldo negativo, sem categoria, lançamento de outro participante; na Parte 2, saldo baixo e gasto acima do normal) | Recorrentes (extraído do código da Parte 1) | Lançamento manual, ocorrências geradas, Parte 2 |
| **C5. Verificação ao abrir o app** | `POST api/Verificacao`, chamado pelo front; roda no máximo 1×/hora por usuário uma lista de passos; falha num passo não bloqueia nada | Recorrentes (passo "gerar ocorrências") | Parte 2 (passos atrasada, vencimento, prazo) |
| **C6. Anti-repetição** | Garantir que o mesmo item não seja criado duas vezes: ocorrência única por série+data; aviso único por `Chave` | Recorrentes (série+data) e Parte 2 (`Chave`) | — |

### O que mudou nas especificações com essa organização

- **Data futura para PENDENTE** e **"hoje" em Brasília** saíram da Parte 2 e passaram para
  Recorrentes (C2, C3).
- O gatilho das verificações deixou de ser o `GET api/Notificacao` (Parte 2) e virou um endpoint
  próprio, `POST api/Verificacao` (C5), porque gerar transações não é papel da listagem de
  notificações.
- O controle "última verificação" saiu da tabela de preferências da Parte 2 e virou um controle
  em memória do próprio mecanismo C5 (se a API reiniciar, verifica de novo — sem problema, porque
  tudo é protegido por C6).
- Os avisos que a Parte 1 colocou dentro do `CriarTransacao` vão para C4, para valerem também
  quando uma ocorrência é gerada automaticamente.

## Ordem dos passos na verificação (C5)

1. Gerar ocorrências das séries recorrentes (Recorrentes).
2. Marcar atrasadas e avisar (Parte 2).
3. Lembrete de vencimento (Parte 2) — depois do passo 1, para lembrar também das ocorrências
   recém-geradas.
4. Prazo das metas (Parte 2).

## Decisões (⚑) — adotadas como propostas em 09/10/2026

Você mandou construir sem mudar as propostas; elas valem como decididas. Para mudar alguma,
veja o impacto em [pendências](../../pendencias.md).

| # | Onde | Proposta |
|---|---|---|
| R1 | Recorrentes | Ocorrências são geradas com **até 7 dias de antecedência** (a maior antecedência do lembrete) |
| R2 | Recorrentes | Ocorrência gerada nasce **PENDENTE**, inclusive entradas (salário) |
| R3 | Recorrentes | Série **sem fim** por padrão, com opção "repetir N vezes" (parcelas, mostra "3 de 10") |
| R4 | Recorrentes | Mudar o valor da série fica **fora** (parar e criar outra) |
| R5 | Recorrentes | As 42 transações recorrentes que já existem **não** são convertidas em séries |
| R6 | Recorrentes | Ocorrência gerada **não** gera aviso de "lançamento de outro participante" nem de "sem categoria"; gera saldo negativo (e, na Parte 2, saldo baixo/gasto) |
| R7 | Recorrentes | A série para sozinha se a conta for excluída ou se o autor sair da conta conjunta |
| R8 | Recorrentes / Parte 2 | Pendente **entra no saldo** quando é lançada (regra atual). Com R1, uma conta mensal derruba o saldo até 7 dias antes do vencimento |
| N1 | Parte 2 | Preferências salvas no banco, por usuário |
| N2 | Parte 2 | Padrões dos interruptores iguais aos da tela atual (vencimento 3 dias, atrasada, progresso e prazo ligados; o resto desligado) |
| N3 | Parte 2 | Cada interruptor salva na hora, sem botão Salvar |
| N4 | Parte 2 | Atrasada: status muda sempre; só avisa as dos últimos 30 dias |
| N5 | Parte 2 | Gasto acima do normal usa o mês da data da transação e inclui pendentes |
| N6 | Parte 2 | Lançamento que deixa a conta negativa avisa só "saldo negativo", não "saldo baixo" junto |
| P1 | Parte 1 (pendência, **ainda aberta**) | Corrigir `EditarMeta` para recalcular "concluída" ao mudar o valor alvo? (bug antigo, fora de todas as especificações por enquanto) |
