# Atrasada automática e botão "Conta paga"

**Situação:** implementada e testada em 09/10/2026 (11 verificações de ponta a ponta +
teste do botão no navegador), sem commit.

**Pedido (09/10/2026):** pendente que passa do dia vira ATRASADA sozinha; em "Pendências de
pagamento", botão "Conta paga" com confirmação que muda o status para CONCLUÍDA.

## O que já existia

A verificação ao abrir o app (C5, Parte 2) já marca como ATRASADA toda PENDENTE com data anterior
a hoje (Brasília) e avisa as dos últimos 30 dias. Brechas:

1. Pendente **lançada ou editada** com data passada fica PENDENTE até a próxima verificação.
2. A verificação roda no máximo 1×/hora: na virada do dia pode demorar até 1 hora.
3. O painel carrega as transações junto com a verificação e pode mostrar o status antigo.

## Regras

- **Domínio:** ao criar uma transação PENDENTE com data anterior a hoje, ou ao mudar o status
  para PENDENTE numa transação de data passada, ela vira ATRASADA na hora.
- **Limite da verificação:** roda de novo se passou 1 hora **ou se mudou o dia** em Brasília.
- **Front:** a verificação é uma só por vez (chamadas simultâneas esperam a mesma); o painel
  espera a verificação antes de carregar as transações.
- **Conta paga:** cada item de "Pendências de pagamento" ganha o botão. Saída: "Conta paga" →
  "A conta foi paga?" [Sim, foi paga] [Cancelar]. Entrada: "Recebido" → "O valor foi recebido?"
  [Sim, recebi] [Cancelar]. Confirmar usa o `PUT api/Transacao/{id}` existente com status
  CONCLUÍDA; o item sai das pendências e os painéis recarregam.
- **Permissão:** igual à edição (só o autor, e só enquanto participar da conjunta). O botão só
  aparece nas pendências lançadas pelo próprio usuário **com data até hoje**: conta futura não pode
  ser paga (o backend recusa CONCLUÍDA/ATRASADA em data futura, como na criação).

## Etapas

1. Backend: regra no domínio e limite que reinicia na virada do dia.
2. Front: verificação única e painel esperando por ela.
3. Front: botão "Conta paga" com confirmação.

Cada etapa: build/lint, testes de ponta a ponta (API 5299, membros `tstnotif_*` apagados no fim)
e conferência visual na etapa 3.
