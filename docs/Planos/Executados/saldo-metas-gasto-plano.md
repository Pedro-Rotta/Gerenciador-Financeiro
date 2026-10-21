# Saldo real, saldo inicial, metas e gasto — Plano de implementação

> Execução nativa (executing-plans), tarefa por tarefa, com teste que falha antes de cada mudança.

**Objetivo:** aplicar as regras de [saldo-metas-gasto.md](saldo-metas-gasto.md) (#1, #8, #10).

**Arquitetura:** o saldo continua gravado na conta, mas só transações CONCLUÍDAS mexem nele (no
lançamento, na exclusão e na troca de status, dentro do domínio). Saldo inicial vira dois campos
da conta individual. O previsto é calculado na listagem. As metas passam a ser recalculadas por um
serviço único (`IProgressoDaMeta`). Uma migração cria os campos e recalcula saldos e metas.

**Especificação:** [saldo-metas-gasto.md](saldo-metas-gasto.md)

## Restrições globais

- Commits só quando o usuário pedir (`main`, autor `pedro <pedrorotta2006@gmail.com>`, sem coautoria).
- **Aplicar a migração no banco local só depois de confirmar com o usuário**, com cópia de
  segurança (saldos e metas) antes.
- Padrão do código: use cases + interfaces, repositórios, controllers finos, erros por exceção
  (`InvalidOperation` = 409, `AcessoNegado` = 403). C# em CRLF; front com o fim de linha de cada arquivo.
- Testes de ponta a ponta na API 5299 (cópia do `bin/Release`, `ASPNETCORE_ENVIRONMENT=Development`)
  e Vite 5174; membros `tstnotif_*` apagados no fim; encerrar pelo PID da porta. 5 logins/min.
- Banco: `Tipo` ENTRADA = 0, SAIDA = 1; `Status` PENDENTE = 0, CONCLUIDA = 1, ATRASADA = 2.

## Foco da revisão

1. **Troca de status sem a conta carregada** (verificação diária PENDENTE → ATRASADA): não pode
   tocar no saldo nem quebrar.
2. **"Conta paga" numa conta com saldo baixo:** o aviso de saldo baixo/negativo sai no pagamento.
3. **Excluir transação vinculada a meta concluída:** a meta volta a "não concluída".
4. **Conta sem saldo inicial com série ativa:** a geração da ocorrência não pode derrubar a
   verificação inteira (contas antigas ficam liberadas pela migração; contas novas não têm série).
5. **Saldo inicial negativo** é aceito e aparece no saldo e no previsto.

---

## Etapa 1

### Tarefa 1 (#10): gasto acima do normal só no mês atual

- `AvisosDeLancamento.AvisarGastoAcimaDoNormalAsync`: sair cedo se o mês da transação não for o
  mês atual (`Relogio.HojeBrasilia()`); a mensagem usa sempre "este mês".
- **Teste:** membro de teste com 3 meses de histórico numa categoria e preferência "gasto acima do
  normal" ligada. Lançar despesa alta em mês passado → hoje avisa "em MM/AAAA" (vermelho);
  depois da mudança, não avisa. Despesa alta no mês atual continua avisando.

### Tarefa 2 (#1): metas pela soma real das concluídas

- `Meta.AtualizarValorAtual`: sem teto; `Concluida = ValorAtual >= ValorAlvo`. `RegistrarProgresso`
  sem teto também.
- Novo `IProgressoDaMeta` / `ProgressoDaMeta` (Application/UseCases/Metas): soma as vinculadas
  ativas e CONCLUÍDAS (aceita uma transação sendo vinculada e uma sendo desvinculada), atualiza a
  meta e, se pedido, grava o aviso de progresso (marcos 25/50/75% e conclusão, preferência
  "Progresso da meta"). O `CriarAvisoDeProgresso` sai do `VincularTransacaoMeta` para cá.
- `IMetaTransacaoRepository.ObterPorTransacaoAsync(transacaoId)`.
- Usam o serviço: `VincularTransacaoMeta` (com aviso), `DesvincularTransacaoMeta`, `EditarMeta`
  (quando o alvo muda, sem aviso), `ExcluirTransacao` (vinculada), `EditarTransacao` (status de
  vinculada, com aviso).
- Front: Metas e card do painel mostram "excedeu R$ X" quando `valorAtual > valorAlvo` (barra
  já para em 100%).
- **Testes:** meta alvo 100 + vinculada CONCLUÍDA de 150 → valor 150, concluída (hoje: 100);
  subir alvo para 200 → não concluída, 150; vinculada PENDENTE não soma; pagar a pendente soma e
  avisa; excluir a vinculada recalcula.

## Etapa 2

### Tarefa 3 (#8 domínio): saldo só com CONCLUÍDA

- `IContaTransacionavel`: `AplicarNoSaldo(Transacao)` e `DesfazerNoSaldo(Transacao)`.
- `Conta`/`ContaConjunta.EfetuarTransacao`: adiciona à lista e só mexe no saldo se CONCLUÍDA;
  `EstornarTransacao`: só se CONCLUÍDA.
- `Transacao.AtualizarStatus`: entrar em CONCLUÍDA → `AplicarNoSaldo`; sair → `DesfazerNoSaldo`
  (só nessas transições a conta é usada).
- `IAvisosDeLancamento.AvisarSaldoAsync(transacao, saldoAnterior)` (parte de saldo do `AvisarAsync`),
  chamado pelo `EditarTransacao` quando o status vira CONCLUÍDA.

### Tarefa 4 (#8): saldo inicial

- `Conta`: `SaldoInicial`, `SaldoInicialDefinido`; `DefinirSaldoInicial(valor)` (409 se já
  definido); `EfetuarTransacao` recusa sem saldo inicial (409 "Informe o saldo inicial da conta
  antes de lançar transações.").
- `POST api/Conta/{id}/saldo-inicial` (`DefinirSaldoInicialRequest { Valor }`,
  `IDefinirSaldoInicialUseCase`), só o dono (403).
- `ContaResponse.SaldoInicialDefinido`.

### Tarefa 5 (#8): saldo previsto

- `ITransacaoRepository.SomarPendentesAsync(contaId?, contaConjuntaId?)` (entradas − saídas,
  PENDENTE/ATRASADA, ativas).
- `ContaResponse` e `ContaConjuntaResponse`: `SaldoPrevisto` (`decimal?`, preenchido nas
  listagens).

### Tarefa 6 (#8): migração `SaldoRealESaldoInicial`

- Colunas `SaldoInicial` (0) e `SaldoInicialDefinido` (verdadeiro se a conta tem transação ativa).
- SQL: saldo de contas e conjuntas = saldo inicial + Σ concluídas ativas; metas = Σ vinculadas
  concluídas ativas, `Concluida = ValorAtual >= ValorAlvo`.
- `Down`: tira as colunas e volta saldo/metas à regra antiga (todas as ativas; meta com teto).
- **Antes de aplicar no banco: confirmar com o usuário e salvar cópia** (Id, Saldo das contas e
  conjuntas; Id, ValorAtual, Concluida das metas) num CSV no scratchpad.
- **Testes:** criar membro novo → conta principal pede saldo inicial (transação → 409); definir
  −50 → saldo −50; segunda vez → 409; transação PENDENTE não muda saldo, previsto muda; pagar →
  saldo muda; excluir paga estorna; excluir pendente não; conjunta lança sem saldo inicial.

## Etapa 3

### Tarefa 7 (#8 front)

- Tipos e serviço (`definirSaldoInicial`).
- Painel: botão "Saldo inicial · <conta>" na área de transações para cada conta individual sem
  saldo inicial (modal com o valor, aceita negativo); "Nova transação" desativado sem conta
  liberada; no formulário, contas sem saldo inicial desativadas "(defina o saldo inicial)".
- Card "Saldo total": linha "Previsto: R$ ...". Telas Contas e Contas conjuntas: "Previsto: R$ ..."
  em cada card.
- **Teste no navegador:** membro novo → botão aparece, "Nova transação" desativado; definir →
  botão some, lançar pendente → saldo igual e previsto muda; "Conta paga" → saldo muda.

### Tarefa 8: docs, limpeza e revisão final

- Apagar membros de teste; `pendencias.md` (#1, #8, #10 saem); `estado-do-projeto.md`;
  especificação com a situação; revisão final por agente revisor.
