# Transações recorrentes

**Resumo:** hoje "recorrente" é só uma etiqueta — nada se repete. Esta especificação cria
**séries**: uma transação marcada como mensal ou semanal passa a gerar sozinha as próximas
ocorrências, como contas a pagar/receber pendentes.

**Situação:** implementada e testada em 09/10/2026 (propostas ⚑ adotadas como estão) · **Depende de:** Parte 1 ·
**Usada por:** Notificações Parte 2 · Índice: [roteiro](roteiro-notificacoes-e-recorrencias.md)

Legenda: **✔ você decidiu** · **⚑ proposta minha, para você confirmar**.

## Como funciona hoje (diagnóstico)

- `Recorrente` e `Periodicidade` só são gravados; nenhum código gera a próxima ocorrência.
- No banco: 42 transações recorrentes (todas mensais), cada mês lançado à parte pelo script
  de exemplo. Sem ligação entre elas.
- A marcação não aparece em nenhuma lista, PDF ou Excel e não pode ser editada.
- O que foi lançado é contabilizado certo (uma vez cada); o problema é o que **não** foi
  lançado: saldo, relatórios e "Pendências de pagamento" não enxergam os próximos meses.

## Escopo

**Entra**
- Série recorrente mensal ou semanal, criada pelo formulário atual de nova transação.
- Geração automática das próximas ocorrências como PENDENTE.
- Série com fim opcional ("repetir N vezes" = parcelas).
- Etiqueta da recorrência nas listas de transações.
- Parar de repetir; excluir "só esta" ou "esta e as próximas".
- Peças comuns C2, C3, C4 e C5 (ver [roteiro](roteiro-notificacoes-e-recorrencias.md)).

**Não entra**
- Mudar valor, data ou categoria de uma série já criada (⚑ R4: parar e criar outra).
- Converter as 42 recorrentes existentes em séries (⚑ R5).
- Outras periodicidades (quinzenal, anual) — o enum continua MENSAL/SEMANAL.
- Mostrar recorrência no PDF/Excel.

## Decisões

| # | Decisão | Escolha |
|---|---|---|
| — | Construir antes da Parte 2, como trabalho separado | ✔ |
| — | Geração roda ao abrir o app (mesmo mecanismo das verificações da Parte 2) | ✔ (escolha da Parte 2, reaproveitada) |
| — | Pendente com data futura é permitida; entra no saldo ao ser lançada | ✔ (escolha da Parte 2) |
| R1 | Gerar ocorrências com até **7 dias de antecedência** | ⚑ |
| R2 | Ocorrência gerada nasce **PENDENTE**, inclusive entradas; o usuário marca CONCLUIDA quando pagar/receber | ⚑ |
| R3 | **Sem fim** por padrão; opção "repetir N vezes" (2 a 120), etiqueta "3 de 10" | ⚑ |
| R4 | Mudar a série fica fora; para trocar, parar e criar outra | ⚑ |
| R5 | As 42 existentes continuam como etiqueta (não viram série) | ⚑ |
| R6 | Ocorrência gerada roda só o aviso de **saldo negativo** (e, na Parte 2, saldo baixo e gasto); não avisa "lançamento de outro participante" nem "sem categoria" | ⚑ |
| R7 | A série para sozinha se a conta for excluída ou se o autor sair da conta conjunta | ⚑ |
| R8 | Com R1 + regra atual de saldo, uma conta mensal derruba o saldo até 7 dias antes do vencimento | ⚑ (consequência; alternativa seria a pendente não entrar no saldo — mudança grande) |

## Peças em comum

| Peça | Papel aqui |
|---|---|
| C1 Tipo/gravidade + `AvisosNotificacao` | Reutiliza (aviso de saldo negativo das ocorrências) |
| **C2 "Hoje" em Brasília** | **Cria**: `Relogio.HojeBrasilia()` no `Domain/Shared`, usado pela regra de data da transação e pela geração |
| **C3 Data futura para PENDENTE** | **Cria**: muda a regra do construtor de `Transacao` |
| **C4 Avisos ao lançar** | **Cria**: extrai do `CriarTransacao` os avisos da Parte 1 para `AvisosDeLancamento`, chamado no lançamento manual e na geração |
| **C5 Verificação ao abrir o app** | **Cria**: `POST api/Verificacao` com o passo "gerar ocorrências" |
| **C6 Anti-repetição** | **Cria** a parte da série: índice único (`RecorrenciaId`, `DataTransacao`) |

## Dados

### Nova entidade `Recorrencia` (tabela `Recorrencias`) — o molde da série

| Campo | Tipo | Observação |
|---|---|---|
| `Valor`, `Descricao`, `Tipo`, `Classificacao` | iguais aos da transação | copiados para cada ocorrência |
| `Periodicidade` | MENSAL / SEMANAL | |
| `MembroId` | int | autor da série = autor das ocorrências |
| `ContaId` / `ContaConjuntaId` | int? | exatamente um |
| `CategoriaId` / `SubCategoriaId` | int? | |
| `DataInicio` | DateOnly | data da 1ª ocorrência |
| `DiaDoMes` | int | só mensal; dia da `DataInicio` |
| `ProximaData` | DateOnly | data da próxima ocorrência a gerar |
| `TotalOcorrencias` | int? | null = sem fim (R3) |
| `OcorrenciasGeradas` | int | inclui a 1ª |
| `Ativa` | bool | false = parou de repetir |

### `Transacao` ganha

- `RecorrenciaId` (int?, FK) e `NumeroOcorrencia` (int?, 1, 2, 3...).
- Índice único (`RecorrenciaId`, `DataTransacao`) quando `RecorrenciaId` não é nulo (C6).

`Recorrente` e `Periodicidade` continuam existindo (as 42 antigas dependem deles); nas
ocorrências novas são preenchidos a partir da série.

## Regras

**Data futura (C3)** — o construtor de `Transacao` aceita data futura só com status
PENDENTE; os demais continuam limitados a hoje ("hoje" = Brasília, C2). A edição segue a
mesma regra (decisão de 09/10): **não dá para marcar como CONCLUÍDA (nem ATRASADA) uma transação
com data futura** — a conta só pode ser paga a partir do dia dela.

**Criar a série** — no formulário atual, ao marcar "Transação recorrente":
- O `CriarTransacao` cria a `Recorrencia` e a **1ª ocorrência** com a data e o status
  escolhidos (como hoje). `ProximaData` = próxima data depois da 1ª.
- Novo campo opcional "Quantidade de vezes" (vazio = sem fim).

**Calcular a próxima data**
- Semanal: + 7 dias.
- Mensal: mesmo `DiaDoMes`; se o mês não tiver esse dia (31 em abril, 30/31 em fevereiro),
  usa o último dia do mês, e volta ao dia original no mês seguinte.

**Gerar ocorrências** (passo da verificação C5, para as séries ativas das contas que o
usuário acessa — próprias e conjuntas de que participa):
- Enquanto `ProximaData ≤ hoje + 7 dias` (R1) e a série não atingiu `TotalOcorrencias`:
  cria a ocorrência PENDENTE (R2) com `ProximaData`, chama C4 (R6), avança `ProximaData`.
- Se o app ficou dias sem ser aberto, gera todas as ocorrências que faltaram de uma vez
  (as já vencidas entram como PENDENTE; na Parte 2 viram ATRASADA).
- Ao atingir `TotalOcorrencias`, a série fica inativa.
- Série cuja conta foi excluída, ou cujo autor saiu da conta conjunta, fica inativa (R7).
- Categoria ou subcategoria excluída: a ocorrência é gerada sem ela.

**Parar de repetir** — inativa a série; ocorrências já geradas ficam como estão.

**Excluir uma ocorrência de série** — a confirmação oferece:
- **Só esta** (padrão): exclui a ocorrência (com estorno, como hoje); a série continua e
  **não recria** essa data (C6 + `ProximaData` já avançou).
- **Esta e as próximas**: exclui esta e as ocorrências **pendentes futuras** da série e para
  a série.

**Editar uma ocorrência** — igual a hoje (descrição e status, só dela).

**Permissões** — iguais às da transação: só o autor da série para/exclui; em conta conjunta,
só enquanto participar.

## API

| Método | Rota | Uso |
|---|---|---|
| POST | `api/Transacao` | Aceita `quantidadeOcorrencias?`; com `recorrente = true` cria a série + 1ª ocorrência |
| POST | `api/Verificacao` | Verificação ao abrir o app (C5); responde 204 |
| POST | `api/Transacao/{id}/parar-recorrencia` | Para a série da ocorrência |
| DELETE | `api/Transacao/{id}?escopo=0\|1` | 0 = só esta (padrão), 1 = esta e as próximas (enum trafegado como número) |
| GET | `api/Transacao` | Resposta ganha `recorrenciaId`, `numeroOcorrencia`, `totalOcorrencias`, `serieAtiva` |

## Tela

- **Formulário de nova transação:** com "Transação recorrente" marcado, além de
  Mensal/Semanal, campo "Quantidade de vezes (opcional)".
- **Listas** ("Transações recentes" e "Gerenciar transações"): etiqueta "Mensal" ou
  "Semanal", e "3 de 10" quando a série tem fim; "Encerrada" se a série parou.
- **Gerenciar transações:** em ocorrências de série ativa, botão "Parar de repetir";
  na confirmação de exclusão, as opções "Só esta" e "Esta e as próximas".
- O front chama `POST api/Verificacao` ao carregar o app e junto da atualização periódica do
  sino (o servidor limita a 1×/hora). Ocorrências geradas aparecem nas listas ao recarregar.

## Etapas

1. **Base** — C2 (hoje em Brasília), C3 (data futura para PENDENTE), entidade `Recorrencia`,
   migração, criar série pelo formulário (com quantidade de vezes), etiquetas nas listas.
2. **Geração** — C5 (`POST api/Verificacao` + limite de 1×/hora), passo "gerar ocorrências",
   C4 (extrair os avisos da Parte 1 para `AvisosDeLancamento`), chamada no front.
3. **Gerenciar** — parar de repetir, excluir "só esta / esta e as próximas", fim da série
   por quantidade, série inativa por conta excluída / autor fora da conta.

Ao fim de cada etapa: análise de padrão/qualidade; se algo quebrar o padrão, parar e avisar.

## Verificação

`dotnet build`, `npx tsc -b`, `npx oxlint src`; teste de ponta a ponta com API temporária
(porta 5299) e membros `tstnotif_*` apagados ao final, cobrindo: mensal com dia 31 em
fevereiro, semanal, geração atrasada de vários períodos, "repetir 3 vezes", parar,
excluir "só esta" sem recriar, excluir "esta e as próximas", chamada dupla de verificação
sem duplicar; conferência visual das telas.

## Ajustes feitos na implementação

- **Prévia das mensais** (pedido de 09/10): `GET api/Recorrencia/previstas` lista as ocorrências
  das séries **mensais** (com parcelas ou sem fim) até o **fim do próximo mês** que ainda não foram
  lançadas. Aparecem em "Pendências de pagamento" como "Prevista · 6 de 10" / "Prevista · Mensal",
  **fora do saldo** (a transação de verdade continua sendo lançada 7 dias antes). Previstas são
  futuras, então **não têm "Conta paga"**: só podem ser pagas depois de lançadas e no dia. Semanais
  ficam de fora.

- **Gera na hora de criar a série** (não espera a verificação). Ocorrências com data **anterior ao
  dia em que a série foi criada** entram como **CONCLUÍDA** (histórico já pago); a partir desse
  dia, PENDENTE (e viram ATRASADA se passarem do dia). Ex.: criada em 09/10 com início em 09/05 →
  09/06 a 09/09 concluídas, 09/10 pendente. Vale também para séries antigas geradas depois.
- **Esta e as próximas** exclui as seguintes **não concluídas** (pendentes ou atrasadas), não só as
  pendentes: com a Parte 2, as ocorrências vencidas viram ATRASADA e ficariam de fora.
- **Etiqueta** ("Mensal", "3 de 10", "Encerrada") só nas ocorrências de série; as 42 antigas (R5)
  continuam sem etiqueta, para não parecer que se repetem.
- **Parar de repetir** em conta conjunta avisa os outros participantes (com o interruptor "Lançamentos
  de outros participantes" ligado).
- **R7** acontece na hora (excluir conta, sair ou ser removido da conjunta) e também na geração.

## Riscos e pontos de atenção

- **Saldo antecipado (R8):** com geração 7 dias antes e a regra atual, o saldo mostra a conta
  como já paga antes do vencimento. Se isso incomodar, a alternativa (pendente fora do saldo)
  é uma mudança grande em saldo, estorno e relatórios — tratar à parte.
- **Primeira geração em massa:** séries criadas há muito tempo sem o app aberto geram várias
  ocorrências de uma vez (todas pendentes; na Parte 2, atrasadas além de 30 dias não avisam).
- **Dados de exemplo:** as 42 recorrentes antigas não viram séries (R5); se quiser, dá para
  recriar as principais (Aluguel, Salário...) como séries depois.
