# Estado do projeto — GestorFinanceiroCSharp

Ponto de partida para um novo chat (atualizado em 14/10/2026). Leia este arquivo e
as [pendências](pendencias.md). Especificações e planos ficam em [Planos/](Planos/):
[Executados/](Planos/Executados/) (já implementados, só como histórico) e
[Pendentes/](Planos/Pendentes/) (propostos, ainda sem implementação). Plano novo nasce em
`Pendentes/` e vai para `Executados/` quando for implementado.

## Stack

- **Backend:** ASP.NET Core 8 + EF Core 8 (PostgreSQL), Clean Architecture
  (Domain / Application / Infrastructure / Web). JWT; os controllers pegam o `MembroId` do
  token. Erros: `ArgumentException` = 400, `UnauthorizedAccess` = 401,
  `AcessoNegadoException` = 403, `KeyNotFound` = 404, `InvalidOperation` = 409.
- **Frontend:** React 19 + TypeScript + Vite (porta 5173, proxy para a API em 5237), axios,
  componente `Modal`, CSS em `src/index.css`. Checagem: `npx tsc -b` e `npx oxlint src`.
- **PDFs:** QuestPDF no backend. **Excel:** ExcelJS no front (sob demanda).
- Sem projeto de testes: verificação por build, lint e testes de ponta a ponta via API.

## Regras combinadas

- **Commits** direto na `main`, autor `pedro <pedrorotta2006@gmail.com>`
  (`git -c user.name=pedro -c user.email=pedrorotta2006@gmail.com commit`), mensagem em
  português, **sem** "Co-Authored-By: Claude". Commit só quando pedido; **o push é do usuário**.
- **Manter o padrão do código:** DTOs, use cases + interfaces, repositórios, controllers finos;
  enums aninhados na entidade e trafegados como número.
- **Fluxo de trabalho:** funcionalidade nova → especificação em `docs/` para o usuário revisar →
  plano → execução em **3 etapas**, com análise de padrão/qualidade ao fim de cada uma (se
  quebrar o padrão, parar e avisar).
- **Pendências:** toda pendência nova vai na hora para [pendencias.md](pendencias.md) (direto e prático).
- **Não alterar configurações de segurança**; os segredos ficam no User Secrets (nunca exibir).
- **Limite de uso:** conferir o uso antes e durante as tarefas e **parar com qualquer limite
  em 95%**.
- **Nada estático/fictício** na interface, exceto pré-visualização pedida (aba Configurações).
- **Login** diferencia maiúsculas no nome de usuário (decisão do usuário).
- **Servidores:** o usuário sobe backend e front. Testes usam servidores temporários (API em
  5299, Vite em 5174), encerrados ao final — matar só pelo PID da porta, nunca pelo nome do
  processo (o backend do usuário pode estar rodando).
- **Dados de teste:** membros temporários com prefixo `tstnotif_`, apagados ao final por SQL com
  trava para usuários reais (que não são listados aqui).

## Ambiente (dicas)

- **Banco:** PostgreSQL 18 como serviço do Windows (`postgresql-x64-18`), connection string com
  `Host=localhost` (o IP antigo 172.19.160.1 do WSL sumiu e derrubava a API).
- **Segredos** (connection string e chave do JWT) ficam no **User Secrets**
  (`%APPDATA%\Microsoft\UserSecrets\0f265c64-571e-4a63-933f-53b752da7246\secrets.json`), não no
  `appsettings.json`. Ver só os nomes: `dotnet user-secrets list | ForEach-Object { ($_ -split ' = ')[0] }`
  na pasta `GerenciadorContaFamiliar`. A API para na inicialização se faltar algum.
  **Gravar segredos sempre pelo terminal do usuário:** o que o Claude grava pelas próprias
  ferramentas no `%APPDATA%` fica no sandbox dele e não chega ao usuário (aconteceu em 12/10).
- Sem `psql`: consultas por um pequeno app Npgsql que lê a connection string do `secrets.json`
  (nunca exibir).
- Migrações: `dotnet tool run dotnet-ef ... --project GerenciadorContaFamiliar.Infrastructure
  --startup-project . --configuration Release` (o backend rodando trava o `bin/Debug`).
- Arquivos com **CRLF** e sem BOM: o `sed -i` do Git Bash converte para LF; em edição em massa
  usar PowerShell (`[IO.File]::ReadAllText/WriteAllText` com `UTF8Encoding($false)`).
- Rota `/` do front sempre redireciona para `/login`; a área logada começa em `/dashboard`.
- Testes de ponta a ponta: rodar a API temporária a partir de uma **cópia** do `bin/Release` (com
  `--contentRoot` no projeto e `ASPNETCORE_ENVIRONMENT=Development`, para ler o User Secrets),
  senão ela trava o próximo build. O Vite de teste (5174) usa uma config temporária apontando o
  proxy para a 5299, sem mexer no `vite.config.ts`.
- **Sessão em cookie:** o login grava o cookie `sessao` (HttpOnly); com curl, usar cookie jar
  (`-c`/`-b`). O login aceita **5 tentativas por minuto por IP** (429 depois): espaçar os logins
  dos testes. O cabeçalho `Authorization: Bearer` continua aceito (Swagger).

## Estado atual

Último commit de código: `602ec72` (segurança), na `main` local — **o push é do usuário**.
Banco local com todas as migrações aplicadas (a última: `SaldoRealESaldoInicial`, 13/10).
**O backend do usuário precisa ser reiniciado** para carregar o código novo (e todos precisam
logar de novo: a chave do JWT mudou).

Feito e testado em 09/10 (testes de ponta a ponta + conferência visual):

| Funcionalidade | Resumo |
|---|---|
| **Transações recorrentes** | Séries mensais/semanais (`Recorrencia`), "quantidade de vezes" (parcelas), geração **na hora de criar** e na verificação ao abrir o app (até 7 dias à frente); datas anteriores ao dia da criação entram como CONCLUÍDA (histórico); parar de repetir; excluir "só esta / esta e as próximas"; série para ao excluir a conta ou sair da conjunta |
| **Prévia das mensais** | `GET api/Recorrencia/previstas`: mês atual + próximo em "Pendências de pagamento" ("Prevista · 3 de 5"), **fora do saldo** e sem "Conta paga" |
| **Notificações Parte 2** | Preferências por membro (aba Notificações real), saldo baixo, gasto acima do normal, atrasada, lembrete de vencimento, prazo de meta; `Chave` única contra repetição |
| **Atrasada automática** | PENDENTE com data passada vira ATRASADA na hora (criar/editar) e na virada do dia |
| **Conta paga** | Botão com confirmação em "Pendências de pagamento"; **conta com data futura não pode ser paga** (backend recusa CONCLUÍDA/ATRASADA em data futura) |

Peças técnicas: `Relogio.HojeBrasilia()`, `POST api/Verificacao` com passos (`IPassoVerificacao`,
máx. 1×/hora ou na virada do dia), `AvisosDeLancamento`, `GeradorDeOcorrencias`.

### Segurança (12/10, commit `602ec72`)

Especificação e plano em [seguranca.md](Planos/Executados/seguranca.md) e
[seguranca-plano.md](Planos/Executados/seguranca-plano.md).
Feito e testado (API 5299 + navegador no Vite 5174): segredos no User Secrets com chave nova do JWT
(antiga #11), limite de 5 tentativas/minuto no login (antiga #12), sessão em cookie `HttpOnly` com
`POST api/Auth/logout` e front sem token (antiga #14), Postman sem `Bearer`. Todas as sessões antigas
caíram (é preciso logar de novo).

### Saldo real, saldo inicial, metas e gasto (13/10, sem commit)

Especificação e plano em [saldo-metas-gasto.md](Planos/Executados/saldo-metas-gasto.md) e
[saldo-metas-gasto-plano.md](Planos/Executados/saldo-metas-gasto-plano.md). Feito e testado (API 5299 + navegador):

| Regra | Resumo |
|---|---|
| **Saldo real** (antiga #8) | Saldo = saldo inicial + entradas − saídas **CONCLUÍDAS**; pendente entra ao ser paga ("Conta paga"), sair de CONCLUÍDA desfaz, excluir só estorna a concluída; aviso de saldo baixo/negativo no pagamento |
| **Saldo previsto** | `saldoPrevisto` na listagem de contas e conjuntas (com pendentes/atrasadas); card "Saldo total" e cards das telas de contas |
| **Saldo inicial** | Conta individual nova: botão "Saldo inicial · conta" no painel, `POST api/Conta/{id}/saldo-inicial` (uma vez, aceita negativo); sem ele a conta recusa transações (409); conjunta começa em 0 |
| **Metas** (antiga #1) | Soma real das vinculadas **concluídas**, sem teto ("excedeu R$ X"); recalcula ao vincular, desvincular, mudar o alvo, excluir ou pagar uma vinculada (`IProgressoDaMeta`) |
| **Gasto acima do normal** (antiga #10) | Só no mês atual |

**Relatórios pelo saldo real (14/10, antigas #24 e #27):** resumo, gastos por categoria, PDF e
Excel dos relatórios, cards "Recebidas/Pagas hoje" e "Gastos por categoria" do painel e os totais do
card na tela Contas contam só transações CONCLUÍDAS. Continuam com todas: a tabela de transações, o
PDF da lista de transações, "Pendências de pagamento" e o aviso de gasto acima do normal.

**Ordenação da tabela de transações (14/10):** setas nos 5 títulos do painel
(Descrição A-Z, Categoria A-Z, Conta principal → A-Z, Data mais recente, Valor maior); clique: padrão
→ invertido → desmarcado; várias marcadas viram uma só ordenação (`Array.sort`), a marcada primeiro
decide e as outras desempatam. O PDF da lista recebe `ordenacao=conta,valor:inv` e sai na mesma ordem
(`OrdenacaoDeTransacoes` no backend, mesmas regras de `utils/ordenacaoTransacoes.ts`) e mostra a ordem
no cabeçalho.

**Esqueceu a senha e alterar senha (14/10, sem commit):** especificação e plano em
[recuperar-senha.md](Planos/Executados/recuperar-senha.md) e [recuperar-senha-plano.md](Planos/Executados/recuperar-senha-plano.md).
Link "Esqueceu a senha? Nova senha" no login → `/recuperar-senha` (código de 6 dígitos por e-mail,
15 min, 5 erros, só o hash guardado, resposta igual exista ou não a conta); "Alterar senha" na aba
Conta das Configurações, embaixo de Telefone. Trocar a senha derruba as sessões (`Membro.VersaoSessao`
no JWT, conferida a cada requisição). Limite: 5 pedidos de senha a cada 10 min por IP. E-mail por
SMTP (MailKit); **sem SMTP configurado, em desenvolvimento o código vai para o console da API**
(pendência #3). Testes de ponta a ponta leem o código no log da API.

Peças técnicas: `IContaTransacionavel.AplicarNoSaldo/DesfazerNoSaldo`, `Transacao.AtualizarStatus`
mexe no saldo só ao entrar/sair de CONCLUÍDA, `IAvisosDeLancamento.AvisarSaldoAsync`,
`ITransacaoRepository.SomarEmAbertoAsync`.

## Testes automatizados

Projeto `GerenciadorContaFamiliar.Tests` (xUnit + NSubstitute, na solução), sem banco: regras do
domínio e casos de uso de membro com repositórios falsos. Rodar na raiz do repositório:
`dotnet test GerenciadorContaFamiliar.Tests`. O que falta cobrir está na pendência #4.

## Banco zerado (14/10/2026)

A pedido do usuário, todas as tabelas foram apagadas e as migrações reaplicadas (banco vazio, sem
membros). Backup completo de antes em `backups/GerenciadorFamiliar-2026-10-14-antes-de-apagar.dump`
(fora do git; restaurar com `pg_restore`).

## Próximos passos

1. Feito em 15/10/2026 pelo usuário: senha do `postgres` trocada (a antiga, do histórico do git, é
   recusada) e PostgreSQL fechado para a rede (escuta só em `127.0.0.1`/`::1`; linha `172.19.160.0/20`
   removida do `pg_hba.conf`; nenhuma regra de firewall de entrada na 5432). Próximo grande passo
   pedido: publicar o app num servidor — planejar antes (hospedagem, HTTPS, e-mail, #15–#17).
2. Reiniciar o backend e revisar no app (séries, prévia, Conta paga, aba Notificações, login novo).
3. Revisar no app as Configurações (todas as abas reais agora), a exportação "Meus dados" e os
   PDFs com símbolos — [correcao-pendencias-menores.md](Planos/Executados/correcao-pendencias-menores.md)
   (#6, #7, #8 e #9 resolvidas; #21 é decisão do usuário). Migração `PreferenciasDeUso` aplicada.
4. Revisar no app o saldo real, o saldo inicial (contas vazias vão pedir) e as metas;
   commitar quando aprovado.

## Pendências

Ficam em [pendencias.md](pendencias.md): toda pendência nova é registrada lá na hora.

## Histórico resumido

- 07–08/10: exclusão com estorno, permissões 403, sair/remover de conta conjunta, filtros,
  relatórios com dados reais, sino só com não lidas, Excel formatado, PDFs no backend, tela de
  Configurações (pré-visualização), confirmação de senha no cadastro (até `c6e295b`).
- 08–09/10: audit, Notificações Parte 1, correção do host do banco, diagnóstico das
  recorrências e especificações de Recorrentes e Parte 2 (commits até `1d060ca`).
- 09/10: Transações recorrentes, Notificações Parte 2, atrasada automática, Conta paga, geração
  imediata das séries e prévia das mensais (commits até `af9d1b1`); especificações movidas para
  `docs/Desnecessários/`; discussão da integração com bancos.
