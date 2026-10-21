# Notificações — Parte 2 (aba de Configurações funcionando)

**Resumo:** os itens "Em breve" da aba **Configurações → Notificações** passam a funcionar e
a ser salvos por usuário: interruptores, lembrete de vencimento, transação atrasada, saldo
baixo, gasto acima do normal, prazo de meta e horário de silêncio.

**Situação:** implementada e testada em 09/10/2026 (propostas ⚑ adotadas como estão) · **Depende de:** Parte 1 e Transações
recorrentes · Índice: [roteiro](roteiro-notificacoes-e-recorrencias.md)

Legenda: **✔ você decidiu** · **⚑ proposta minha, para você confirmar**.

## Escopo

| Item da aba | Hoje | Depois |
|---|---|---|
| Lançamentos de outros participantes | Aviso existe (Parte 1), sem interruptor | Respeita o interruptor de quem recebe |
| Lembrete de vencimento (1, 3 ou 7 dias antes) | Não existe | Novo, na verificação ao abrir o app |
| Transação atrasada | Não existe | Novo, na verificação (marca ATRASADA) |
| Saldo baixo (valor definido pelo usuário) | Não existe | Novo, ao lançar |
| Gasto acima do normal | Não existe | Novo, ao lançar |
| Progresso da meta | Aviso existe (Parte 1), sem interruptor | Respeita o interruptor do dono |
| Prazo chegando (30 dias) | Não existe | Novo, na verificação |
| Horário de silêncio (22h às 7h) | Só visual | Preferência salva; **não afeta o sino** |

**Não entra**
- Canais (e-mail e navegador): continuam "Em breve".
- Resumos (semanal e mensal): a seção **sai da tela**.
- Interruptor para avisos de categoria e de renomear/encerrar conta conjunta (Parte 1):
  continuam sempre ligados.

## Decisões

| # | Decisão | Escolha |
|---|---|---|
| — | Só os itens listados acima; Resumos sai da tela | ✔ |
| — | Saldo baixo: **um valor único** por usuário, vale para contas individuais e conjuntas | ✔ |
| — | Gasto acima do normal: **30% acima da média** dos 3 meses anteriores, fixo | ✔ |
| — | Verificações de tempo rodam **ao abrir o app**, não em segundo plano | ✔ |
| — | Pendente pode ter data futura; entra no saldo ao ser lançada | ✔ (construído em Recorrentes, C3) |
| — | Horário de silêncio **não segura o sino**; vale só para e-mail/navegador quando existirem | ✔ |
| N1 | Preferências salvas **no banco, por usuário** | ⚑ |
| N2 | Padrões (iguais aos da tela atual): lançamentos de outros **desligado**, vencimento **ligado, 3 dias**, atrasada **ligado**, saldo baixo **desligado**, gasto **desligado**, progresso **ligado**, prazo **ligado**, silêncio **desligado** | ⚑ |
| N3 | Cada interruptor **salva na hora** (sem botão Salvar) | ⚑ |
| N4 | Atrasada: o status vira ATRASADA **sempre**; só **avisa** as dos últimos 30 dias (evita inundar o sino na 1ª vez) | ⚑ |
| N5 | Gasto acima do normal conta o **mês da data da transação** e inclui pendentes | ⚑ |
| N6 | Se o lançamento deixa a conta negativa, avisa só **saldo negativo** (não saldo baixo junto) | ⚑ |

## Peças em comum

| Peça | Papel aqui |
|---|---|
| C1 Tipo/gravidade + `AvisosNotificacao` | Reutiliza |
| C2 "Hoje" em Brasília | Reutiliza (datas de vencimento, atraso e prazo) |
| C3 Data futura para PENDENTE | Reutiliza (é o que dá sentido ao lembrete de vencimento) |
| C4 Avisos ao lançar | **Amplia**: + saldo baixo, + gasto acima do normal, + respeitar interruptores. Vale também para ocorrências geradas pelas séries |
| C5 Verificação ao abrir o app | **Amplia**: + passos atrasada, vencimento e prazo (depois do passo "gerar ocorrências") |
| **C6 Anti-repetição** | **Cria** a `Chave` da notificação |

## Dados

### Nova entidade `PreferenciaNotificacao` (tabela `PreferenciasNotificacao`)

Uma linha por membro (`MembroId` único), criada na primeira vez que ele salva; sem linha,
valem os padrões (N2).

| Campo | Tipo | Padrão |
|---|---|---|
| `LancamentosOutros` | bool | false |
| `Vencimento` | bool | true |
| `DiasAntesVencimento` | int (1, 3 ou 7) | 3 |
| `Atrasada` | bool | true |
| `SaldoBaixo` | bool | false |
| `ValorSaldoBaixo` | decimal? | null |
| `GastoAcimaNormal` | bool | false |
| `MetaProgresso` | bool | true |
| `MetaPrazo` | bool | true |
| `HorarioSilencio` | bool | false |

Regras no domínio: `DiasAntesVencimento` só aceita 1, 3 ou 7; com `SaldoBaixo` ligado,
`ValorSaldoBaixo` é obrigatório e maior que zero.

### `Notificacao` ganha `Chave` (string?, até 100 caracteres) — C6

Índice único (`MembroId`, `Chave`) quando `Chave` não é nula. A chave é procurada entre todas
as notificações do membro, lidas ou não.

| Aviso | Chave |
|---|---|
| Vencimento | `vencimento:{transacaoId}` |
| Atrasada | `atrasada:{transacaoId}` |
| Prazo chegando | `prazo:{metaId}` |
| Gasto acima do normal | `gasto:{categoriaId}:{aaaa-MM}` |

## Regras

Destinatários: conta individual → o dono; conta conjunta → **cada participante conforme a
própria preferência** (inclusive quem lançou, nos avisos de saldo e gasto).

### Ao lançar (C4 — lançamento manual e ocorrência gerada)

- **Saldo baixo** — SALDO / ATENCAO. Saldo passa de `≥ ValorSaldoBaixo` para
  `< ValorSaldoBaixo`. Se fica negativo, só o aviso de saldo negativo (N6). Só ao criar;
  excluir uma entrada não avisa (igual ao saldo negativo da Parte 1).
  Ex.: `O saldo da conta "Corrente" ficou abaixo de R$ 200,00: R$ 150,00.`
- **Gasto acima do normal** — TRANSACAO / ATENCAO. Só saída com categoria. Total do mês da
  transação na categoria (saídas ativas, qualquer status) > 1,3 × média dos 3 meses-calendário
  anteriores (meses sem gasto contam zero; exige gasto em pelo menos um deles). Uma vez por
  categoria por mês (chave).
  Ex.: `Gastos em "Comida" este mês: R$ 900,00, 50% acima da média dos últimos 3 meses (R$ 600,00).`
- **Lançamentos de outros participantes** e **progresso da meta** (Parte 1): só para quem
  está com o interruptor ligado.

### Na verificação ao abrir o app (C5)

Considera as transações ativas das contas individuais do membro e das conjuntas de que
participa, e as metas ativas dele. "Hoje" = data de Brasília (C2).

- **Atrasada** — TRANSACAO / CRITICA. PENDENTE com data anterior a hoje vira ATRASADA
  (sempre). Com o interruptor ligado, avisa uma vez (chave) as atrasadas com data nos
  últimos 30 dias (N4), inclusive as que já estavam ATRASADA sem aviso.
  Ex.: `A saída "Aluguel" de R$ 1.200,00 está atrasada desde 05/10/2026.`
- **Lembrete de vencimento** — TRANSACAO / ATENCAO. PENDENTE com data entre hoje e hoje +
  `DiasAntesVencimento`. Uma vez por transação (chave). Como as séries geram ocorrências com
  7 dias de antecedência (Recorrentes, R1), o lembrete de 7 dias também as alcança.
  Ex.: `A saída "Luz" de R$ 180,00 vence em 3 dias (12/10/2026).` / `... vence hoje.`
- **Prazo chegando** — META / ATENCAO. Meta ativa, não concluída, prazo entre hoje e hoje +
  30 dias. Uma vez por meta (chave).
  Ex.: `Faltam 20 dias para o prazo da meta "Viagem" e ela está em 45% (R$ 900,00 de R$ 2.000,00).`

Duas verificações simultâneas (duas abas) são seguras: a segunda esbarra no índice da chave,
falha sem efeito e é só registrada no log (C5). Se o membro excluir uma notificação com chave,
o aviso pode voltar na próxima verificação.

## API

| Método | Rota | Uso |
|---|---|---|
| GET | `api/Notificacao/preferencias` | Preferências do membro (padrões se não houver linha) |
| PUT | `api/Notificacao/preferencias` | Salva todas as preferências (corpo completo); inválido → 400 |
| POST | `api/Verificacao` | (C5, criado em Recorrentes) passa a rodar também os passos desta parte |

## Tela (aba Notificações)

- Carrega as preferências ao abrir; cada interruptor e o seletor de dias salvam na hora (N3).
  Se falhar, o controle volta e aparece mensagem de erro.
- **Saldo baixo:** ao ligar, aparece campo em R$; salva ao sair do campo; o interruptor só
  fica ligado com um valor válido.
- Os 8 itens mostram **"Disponível"**.
- **Horário de silêncio:** descrição "Vale para e-mail e notificações do navegador, quando
  estiverem disponíveis. O sino continua normal."
- Seção **Resumos** removida; **Canais** sem mudança.

## Etapas

1. **Preferências** — entidade, migração (tabela + `Chave`), API GET/PUT, aba funcionando,
   interruptores valendo para os avisos da Parte 1; Resumos removido.
2. **Ao lançar** — saldo baixo e gasto acima do normal dentro de C4.
3. **Na verificação** — passos atrasada (com troca de status), vencimento e prazo em C5.

Ao fim de cada etapa: análise de padrão/qualidade; se algo quebrar o padrão, parar e avisar.

## Verificação

`dotnet build`, `npx tsc -b`, `npx oxlint src`; ponta a ponta com API temporária (5299) e
membros `tstnotif_*` apagados ao final, cobrindo cada interruptor ligado/desligado, saldo baixo
× negativo, gasto com e sem histórico, vencimento 1/3/7 dias e "vence hoje", atrasada recente
× antiga, prazo, verificação dupla sem duplicar; conferência visual da aba.

## Ajustes feitos na implementação

- O interruptor **Lançamentos de outros participantes** vale para os avisos de lançar, editar,
  excluir e parar de repetir uma transação da conta conjunta.
- **Convites** e **entrada/saída de participantes** aparecem com o interruptor travado em ligado
  (avisos sempre ligados, sem preferência).
- Lembrete com 1 dia: "vence amanhã (10/10/2026)". Prazo da meta: "O prazo ... é hoje" / "Falta 1 dia".
- Gasto acima do normal fora do mês atual diz o mês: "Gastos em "Lazer" em 09/2026: ...".

## Riscos e pontos de atenção

- **Saldo antecipado** (Recorrentes, R8): ocorrências geradas entram no saldo até 7 dias antes
  do vencimento, o que pode disparar saldo baixo/negativo antes da data da conta.
- **Primeira verificação:** muitas pendentes antigas viram ATRASADA de uma vez (sem aviso para
  as de mais de 30 dias).
- **Silêncio sem efeito visível:** o interruptor salva, mas só fará diferença quando e-mail ou
  notificação do navegador existirem — a descrição na tela deixa isso claro.
