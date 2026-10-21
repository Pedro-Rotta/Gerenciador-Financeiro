# Pendências

Problemas e dívidas encontrados no caminho. Uma linha por item; ao resolver, apagar a linha
(o commit que resolveu fica registrado no git).

Numeração reiniciada em 14/10/2026 (por grupo e importância). Documentos anteriores a essa data
citam a numeração antiga.

## Com o usuário

Só você pode fazer (configuração do computador, do banco ou do e-mail).

| # | Onde | Pendência | Ação sugerida |
|---|---|---|---|
| 3 | **E-mail não funciona ainda** ("Esqueceu a senha") | O app não tem conta de envio configurada: o código de recuperação de senha **não chega por e-mail**; em desenvolvimento ele só aparece no console da API, e fora de Development a API nem sobe sem essa configuração | **Usuário**, no terminal dele, na pasta `GerenciadorContaFamiliar`: `dotnet user-secrets set` para `Email:Host`, `Email:Porta`, `Email:Usuario`, `Email:Senha` e `Email:Remetente` (ex.: Gmail, porta 587, com "senha de app"); depois reiniciar o backend e testar o "Esqueceu a senha". Ver [recuperar-senha.md](Planos/Executados/recuperar-senha.md) |

## Código: correções e melhorias

Pela ordem de importância.

| # | Onde | Pendência | Ação sugerida |
|---|---|---|---|
| 4 | Testes automatizados: só o começo | 77 testes (xUnit) em `GerenciadorContaFamiliar.Tests` cobrem o domínio (saldo, status, saldo inicial, metas, recorrência, conjuntas, senha) e os casos de uso de membro (excluir conta, alterar senha, preferências); o resto dos casos de uso, os controllers e o front não têm teste | Ampliar aos casos de uso de transação/conta/meta; depois integração da API (`WebApplicationFactory`) num banco só de testes |
| 5 | Saldo sem controle de concorrência | Dois participantes pagando ao mesmo tempo na mesma conjunta podem perder o efeito de um pagamento no saldo (já valia para criar/excluir) | Token de concorrência (`xmin`) em `Conta` e `ContaConjunta` |
| 10 | Aviso de meta repetido | Desfazer e refazer o pagamento de uma vinculada repete "meta concluída" e os avisos de 25/50/75% | Guardar uma `Chave` por meta e marco, como nos outros avisos |
| 11 | "Nova transação" se o painel falhar ao carregar | Fica desativado com a dica de saldo inicial, que engana nesse caso | Só desativar quando as contas carregaram e nenhuma está liberada |
| 12 | "Esqueceu a senha": tempo de resposta | Conta existente demora mais (hash + envio do e-mail) que a inexistente: dá para descobrir quem tem conta pelo tempo (o cadastro já revela isso de outro jeito) | Enviar o e-mail em segundo plano (fila) e fazer um hash falso quando a conta não existe |
| 13 | "Esqueceu a senha": envio que falha | Se o reenvio falhar, o código anterior já foi anulado e o novo não chega; a pessoa só vê "Código inválido ou expirado." | Anular os códigos anteriores só depois de enviar com sucesso |
| 14 | Pequenos ajustes da revisão | Checagem `!conta.Ativo` sem efeito em `DefinirSaldoInicial`; uma consulta por conta no previsto e por vinculada na meta; o `Down` da migração `SaldoRealESaldoInicial` perde os saldos iniciais informados | Limpar quando mexer nesses arquivos |
| 25 | Preferências sem aviso de "salvo" | Na aba Preferências (Configurações) cada mudança é salva na hora, mas nada na tela confirma; só o erro aparece | Mostrar um "Salvo" discreto por alguns segundos, como na aba Conta |
| 26 | Notificação do navegador não exercitada | O teste só cobriu o caso de permissão negada (o navegador embutido nega); o aviso de verdade, com permissão concedida, não foi visto | Testar no Chrome/Edge: ligar o interruptor, deixar o app em outra aba e gerar uma notificação (ex.: convite) |

## Segurança e produção

Pesam quando o app sair do computador local.

| # | Onde | Pendência | Ação sugerida |
|---|---|---|---|
| 15 | **Segurança:** usuário do banco | A API conecta como `postgres` (superusuário) no PC e como `neondb_owner` (dono do banco) na Render: se um dia entrar uma consulta vulnerável a SQL injection, ela poderia apagar tabelas. Hoje não há nenhuma (só LINQ do EF; `InjecaoSqlTests` confere a parametrização) | Criar um usuário só com leitura/escrita nas tabelas (sem `DROP`/`ALTER`) para a API, e deixar o dono só para as migrações |
| 16 | **Segurança:** CSRF entre portas do localhost | `SameSite` ignora a porta: uma página em outro `localhost:<porta>` pode mandar POST sem corpo (aceitar/recusar convite, sair da conjunta, logout) com o cookie | Recusar métodos de escrita com `Origin` fora do CORS, ou exigir um cabeçalho próprio no `api.ts` |
| 17 | Limite do login atrás de proxy | O limite é por IP; pelo proxy do Vite (e de produção) todos chegam como `127.0.0.1` e dividem as 5 tentativas/minuto | Em produção, configurar `ForwardedHeaders` (só com o IP do proxy como confiável) |
| 18 | Swagger: `Bearer` desatualizado | O botão "Authorize" e o comentário no `Program.cs` falam em token, mas o login não devolve mais o token (o Swagger usa o cookie) | Ajustar o comentário ou tirar o esquema `Bearer` do Swagger |
| 19 | 429 sem `Retry-After` | A resposta do limite do login não diz em quantos segundos tentar de novo (a mensagem já diz "1 minuto") | Preencher o cabeçalho com o metadado `RetryAfter` do limitador |
| 20 | Logout sem rede | Se a chamada de logout falhar, a tela sai mas o cookie segue valendo até expirar (até 60 min) | Tentar de novo ou avisar; relevante em computador compartilhado |
| 29 | **Produção:** rajada que estourou a banda da Render | Em 16/10/2026 (~12h e ~16h–16h37, horário de Brasília) uma enxurrada de requisições gastou ~79 GB de saída (limite grátis: 5 GB) e suspendeu o workspace até o próximo ciclo; os logs mostram centenas de `POST /api/Membro` (cadastro anônimo, **sem limite de requisições**) derrubando as conexões com o Neon, e a instância caiu várias vezes (health check, status 137). Como as respostas da API são pequenas, o grosso da banda deve ter sido as telas/JS (sem compressão), mas sem os "Request logs" (plano pago) não dá para confirmar o caminho. **Não foi SQL injection:** nomes como `dd;DROP DATABASE;` e `dd1'` foram gravados como texto (o log mostra as consultas com parâmetros, `@__nomeDeUsuario_0`); a queda começou às 11h57, 40 min após o primeiro deploy, com as consultas do cadastro levando 4–8 s (provavelmente a CPU de 0,15 tomada pelo BCrypt de cada cadastro e pelas conexões abertas) até a instância cair | **Feito no código** (falta publicar): cadastro limitado a 5/hora por IP, limite global de 300/minuto por IP (telas e API, menos `/api/saude`) e compressão Brotli/Gzip. Falta: a Render voltar (próximo ciclo ou cartão) e publicar; opcional, pôr a Cloudflare na frente (cache das telas, bloqueio de bots) — um ataque distribuído em muitos IPs ainda passa pelo limite por IP |
| 27 | Publicação: ajustes menores da revisão final | `ConexaoBanco.ComTempoMinimo` roda a cada `DbContext` (com migrações desligadas, formato errado dá 409 por requisição em vez de impedir a partida); `/api/saude` com cookie de sessão consulta o banco no `OnTokenValidated`; `HEAD /api/saude` dá 404; aba aberta antes de uma publicação pode pedir o chunk antigo do `exceljs` (404) até recarregar; `.dockerignore` só cobre o `.env` da raiz; `UseDefaultFiles` sem efeito (a raiz sai pelo fallback) | Validar a connection string uma vez na partida; não ler o cookie em `/api/saude`; `[HttpGet, HttpHead]`; "recarregue a página" se o `import('exceljs')` falhar; `**/.env*` no `.dockerignore`; tirar o `UseDefaultFiles` ou corrigir o comentário |

## Decisões e avisos de uso

Sem código agora: decidir ou só saber.

| # | Onde | Pendência | Ação sugerida |
|---|---|---|---|
| 21 | QuestPDF Community | Gratuita só até US$ 1 milhão de receita anual | Decisão do usuário (sem código) — ver [correcao-pendencias-menores.md](Planos/Executados/correcao-pendencias-menores.md) |
| 22 | Postman × cookie `Secure` | A coleção agora depende do cookie `sessao` (`Secure`); não testado se o Postman o envia em `http://localhost` | Testar; se não enviar, usar o Swagger ou um cabeçalho `Bearer` manual |
| 23 | Swagger × front no mesmo navegador | Cookie não separa portas: logar no Swagger (5237) com outro usuário troca a sessão do front (5173), que segue mostrando o nome antigo (as ações dão 403) | Usar outro perfil/janela anônima para o segundo usuário; se incomodar, o front conferir o membro da sessão ao abrir |
| 24 | Front fora de `localhost` em http | Pelo IP da rede (`vite --host`, celular) o cookie `Secure` é recusado: o login "entra" e cai em sessão expirada | Abrir sempre por `localhost` ou HTTPS; se precisar, conferir a sessão logo após o login e avisar |
| 28 | Chave SMTP da Brevo vence em 16/10/2027 | A chave `render` (usada em `Email__Senha` na Render) expira um ano depois de criada; vencida, o "Esqueceu a senha" para de enviar e-mails | Antes do vencimento: Brevo → SMTP & API → SMTP → gerar chave nova → trocar `Email__Senha` na Render → apagar a antiga |
