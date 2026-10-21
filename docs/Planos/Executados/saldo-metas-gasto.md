# Saldo real, saldo inicial, metas e gasto acima do normal

**Situação:** implementada e testada em 13/10/2026 (API de teste + navegador), sem commit.
Migração `SaldoRealESaldoInicial` aplicada no banco local com autorização do usuário; nos dados
reais nenhum saldo nem meta mudou (todas as transações já estavam concluídas) e as contas vazias
de três membros passaram a pedir o saldo inicial.

**Pedido (13/10/2026):** aplicar as decisões das pendências de regra #1 (P1, metas), #8 (R8,
saldo) e #10 (N5, gasto acima do normal).

## O que já existia

- **Saldo** gravado na conta (`Conta.Saldo`, `ContaConjunta.Saldo`), começando em 0. Muda em 3
  pontos: criar transação e gerar ocorrência de série (`AplicarEmConta`) e excluir (`EstornarDaConta`).
  **Todo status** mexe no saldo, inclusive PENDENTE e ATRASADA.
- A edição de transação só muda **descrição e status** (valor, tipo e conta não mudam).
- **Metas:** `ValorAtual` = soma das transações vinculadas ativas (qualquer status), gravada **com
  teto no alvo** (`AtualizarValorAtual`). Editar o alvo não recalcula; excluir uma transação
  vinculada ou mudar o status dela também não.
- **Gasto acima do normal:** compara o total da categoria no **mês da data da transação** com a
  média dos 3 meses anteriores (× 1,3); avisa também meses passados e futuros ("em 08/2026").
- Painel: card "Saldo total" = soma das contas individuais. Tela Contas e Contas conjuntas mostram
  o saldo de cada conta.

## Regras

### #10: gasto acima do normal só no mês atual

- O aviso só é avaliado quando a transação é do **mês atual** (Brasília). Lançamentos de meses
  passados ou futuros não avisam. A mensagem diz sempre "este mês".

### #1: metas pela soma real das concluídas

- `ValorAtual` = soma das transações vinculadas **ativas e CONCLUÍDAS**, **sem teto**.
- `Concluida` = `ValorAtual >= ValorAlvo` (volta a falso se o alvo subir ou o valor cair).
- Recalcula ao: vincular, desvincular, **editar o alvo**, **excluir** uma transação vinculada e
  **mudar o status** de uma transação vinculada.
- Aviso de progresso da meta (preferência "Progresso da meta"): continua ao vincular e passa a sair
  também quando a mudança de status de uma vinculada faz a meta cruzar um marco ou concluir. Editar
  o alvo não avisa (foi o próprio dono que mudou).
- Tela Metas e card do painel: a barra para em 100%; acima disso mostra "excedeu R$ X".
- **Dados atuais:** a migração recalcula `ValorAtual` e `Concluida` de todas as metas pela regra nova.

### #8: saldo real, saldo inicial e previsto

**Saldo**

- Saldo = saldo inicial + entradas CONCLUÍDAS − saídas CONCLUÍDAS (transações ativas).
- Criar transação ou gerar ocorrência: só mexe no saldo se estiver CONCLUÍDA.
- Mudar o status: entrar em CONCLUÍDA aplica o valor; sair de CONCLUÍDA desfaz. PENDENTE ↔ ATRASADA
  não mexe no saldo.
- Excluir: só estorna se a transação estava CONCLUÍDA.
- Avisos de saldo baixo e negativo: também ao mudar o status para CONCLUÍDA ("Conta paga"), com
  o saldo de antes e depois do pagamento.

**Saldo inicial (só conta individual)**

- Campos novos na conta: `SaldoInicial` (decimal) e `SaldoInicialDefinido` (bool).
- `POST api/Conta/{id}/saldo-inicial` com `{ valor }`: só o dono, uma vez só; aceita negativo, zero
  ou positivo. Soma o valor ao saldo e marca como definido. Segunda chamada → 409
  "O saldo inicial desta conta já foi informado.".
- Conta individual sem saldo inicial **recusa transações** (criar e gerar ocorrência) → 409
  "Informe o saldo inicial da conta antes de lançar transações.".
- O saldo inicial **não é transação**: não aparece no extrato nem nos relatórios.
- Conta conjunta: começa em 0, sem saldo inicial (sempre liberada).
- `ContaResponse` ganha `saldoInicialDefinido`.

**Saldo previsto**

- Previsto = saldo + entradas PENDENTES/ATRASADAS − saídas PENDENTES/ATRASADAS (ativas).
- `ContaResponse` e `ContaConjuntaResponse` ganham `saldoPrevisto`.
- Painel: card "Saldo total" com a linha "Previsto: R$ ..." (soma das individuais). Tela Contas e
  Contas conjuntas: "Previsto: R$ ..." no card de cada conta.

**Front: botão "Saldo inicial"**

- Na área de transações do painel, para cada conta individual sem saldo inicial, aparece o botão
  "Saldo inicial · <conta>", que abre um formulário com o valor (aceita negativo) e confirma.
  Depois de salvo, o botão some.
- "Nova transação" fica desativado se nenhuma conta estiver liberada (com uma dica dizendo por quê);
  no formulário, contas sem saldo inicial aparecem desativadas com "(defina o saldo inicial)".

**Migração (dados atuais)**

1. Cria `SaldoInicial` (0) e `SaldoInicialDefinido` nas contas: **verdadeiro** se a conta já tem
   alguma transação, **falso** se não tem nenhuma (vai pedir o valor).
2. Recalcula o saldo de **todas** as contas individuais e conjuntas pela regra nova (só concluídas).
3. Recalcula `ValorAtual` e `Concluida` das metas (regra #1).

**Efeito para quem usa:** os saldos mudam uma vez (as pendentes/atrasadas saem do saldo e passam
para o previsto). Contas sem transação pedem o saldo inicial.

## Etapas

1. #10 e #1 (backend + tela Metas/painel).
2. #8 no backend: domínio, saldo inicial, previsto, avisos no pagamento, migração.
3. #8 no front: botão "Saldo inicial", bloqueio, previsto no painel e nas telas de contas.

Cada etapa: build, `tsc`, `oxlint`, testes de ponta a ponta na API temporária (membros
`tstnotif_*` apagados no fim) e análise de padrão. Etapa 3 com conferência no navegador.
