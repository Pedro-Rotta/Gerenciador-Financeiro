# Segurança: segredos, login, banco e sessão

**Situação:** #11, #12 e #14 implementadas e testadas em 12/10/2026 (API de teste + navegador),
commit `602ec72`. Faltam, com o usuário: trocar a senha do banco (#11b) e aplicar o #13.

**Pedido (12/10/2026):** corrigir as pendências de segurança #11 (segredos no git), #12 (login sem
limite de tentativas), #13 (PostgreSQL aceitando conexões de qualquer interface) e #14 (JWT no
`localStorage`), antes de o projeto crescer.

**Decisões do usuário:** só trocar os segredos, sem reescrever o histórico do git (o repositório é
privado); o banco é acessado só deste Windows (sem WSL); o limite do login é por IP.

## O que já existia

- `appsettings.json` versionado com a senha do banco (usuário `postgres`) e a `SecretKey` do JWT.
  A API já para com erro se faltar a `SecretKey`; a connection string não é conferida.
- `POST api/Auth/login` sem nenhum limite. Senha errada → 401 com `{ mensagem }`.
- `postgresql.conf` com `listen_addresses = '*'` (escuta em `0.0.0.0` e `::`) e `pg_hba.conf`
  liberando `172.19.160.0/20` (faixa do WSL antigo, já sem uso).
- O front guarda o token no `localStorage` e o interceptor do axios o envia no cabeçalho
  `Authorization`. O login devolve `token` no corpo. Front e API ficam na mesma origem pelo proxy do
  Vite (5173 → 5237).
- CORS com as origens 3000, 4200 e 5173 (só a 5173 é usada).

## Regras

### #11: segredos fora do git

- `appsettings.json` continua versionado, com `ConnectionStrings:DefaultConnection` e
  `JwtSettings:SecretKey` **vazios** (a connection string inteira vai para o User Secrets).
- No desenvolvimento os valores vêm do **User Secrets** (`UserSecretsId` no `.csproj`; ficam em
  `%APPDATA%\Microsoft\UserSecrets\`, fora do repositório). Em produção, variáveis de ambiente
  (`ConnectionStrings__DefaultConnection`, `JwtSettings__SecretKey`).
- A API não sobe sem a connection string ou sem a chave do JWT, com mensagem dizendo o que
  configurar (`dotnet user-secrets set ...`). Chave com menos de 32 caracteres também é recusada.
- **Troca dos valores:**
  - Chave do JWT: gerada nova (64 bytes aleatórios em Base64) direto no User Secrets, sem ser
    exibida. Todas as sessões abertas caem e é preciso logar de novo.
  - Senha do banco: **o usuário troca** no pgAdmin (Login/Group Roles → `postgres` → Definition →
    Password) e grava a connection string com
    `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=GerenciadorFamiliar;Username=postgres;Password=<nova>"`
    na pasta `GerenciadorContaFamiliar`.
- Os valores antigos ficam no histórico do git, mas deixam de valer.
- A API de teste (cópia do `bin/Release`) passa a rodar com `ASPNETCORE_ENVIRONMENT=Development`
  para ler o User Secrets; o mini app Npgsql de consultas lê a connection string do User Secrets.

### #12: limite de tentativas no login

- `AddRateLimiter` nativo, política `login`: janela fixa de **5 tentativas por minuto por IP**
  (`RemoteIpAddress`), aplicada só ao `POST api/Auth/login`.
- Acima do limite: **429** com
  `{ mensagem: "Muitas tentativas de login. Aguarde 1 minuto e tente de novo." }`. A tela de login
  já mostra o `mensagem` do erro.
- Contam todas as tentativas (certas e erradas).
- **Limitação conhecida:** pelo proxy do Vite todos chegam como `127.0.0.1`, então no
  desenvolvimento o limite é de todos juntos. Em produção atrás de um proxy é preciso configurar
  `ForwardedHeaders` (nova pendência).

### #13: banco só local (aplicado pelo usuário)

Alterar configuração de segurança do sistema fica com o usuário. Passos (pasta de dados:
`C:\Users\pedro\Suporte em Tarefas\PostgreSQL\18\data`):

1. `postgresql.conf`: `listen_addresses = 'localhost'`.
2. `pg_hba.conf`: apagar a linha `host all all 172.19.160.0/20 scram-sha-256`.
3. Reiniciar o serviço `postgresql-x64-18` (PowerShell como administrador:
   `Restart-Service postgresql-x64-18`).

Depois, conferência só de leitura: a porta 5432 deve escutar apenas em `127.0.0.1` e `::1`, e
nenhuma regra de entrada do firewall deve liberar a 5432.

### #14: sessão em cookie `HttpOnly`

- **Login:** grava o JWT no cookie `sessao` com `HttpOnly`, `Secure`, `SameSite=Strict`,
  `Path=/api` e a mesma validade do token. O corpo da resposta continua com `membroId`, `nome`,
  `nomeDeUsuario`, `email` e `expiraEm`, mas **sem o `token`**.
- **Autenticação:** o `JwtBearer` lê o token do cookie `sessao`; sem cookie, aceita o cabeçalho
  `Authorization: Bearer` (Swagger e scripts).
- **Logout:** novo `POST api/Auth/logout` (anônimo) que apaga o cookie; responde 204.
- **CORS:** só a origem `http://localhost:5173`.
- **Front:**
  - Sai o interceptor que monta o cabeçalho; o navegador envia o cookie sozinho.
  - O `localStorage` guarda só `membroId`, `nome`, `email`, `nomeDeUsuario` e `expiraEm`; estar
    logado = ter `membroId`. O contexto troca `token` por `logado`.
  - "Sair da conta" e o fechamento do aviso de sessão expirada chamam o logout da API (falha de
    rede não impede de sair) e limpam o `localStorage`.
  - Ao abrir o app, apaga a chave `token` antiga, se existir. `emailDoToken` deixa de existir.
  - O 401 sem corpo continua abrindo o aviso de sessão expirada (agora conferindo `membroId`).
- **Postman:** o script do login deixa de salvar `{{token}}` e as requisições deixam de usar
  `Bearer {{token}}`; o Postman guarda e envia o cookie sozinho.
- **Para quem usa:** nada muda, além de logar de novo uma vez.

## Etapas

1. #11: User Secrets, `appsettings.json` sem segredos, checagens na inicialização, chave nova do
   JWT; instruções da senha do banco.
2. #12: limite de tentativas no login.
3. #14: cookie no backend, logout, CORS, front e Postman.

O #13 o usuário aplica quando quiser; a conferência é feita depois.

Cada etapa: build, `npx tsc -b` e `npx oxlint src` (front), testes de ponta a ponta na API
temporária (5299) e análise de padrão/qualidade. Testes da etapa 3: login cria o cookie, requisição
com cookie funciona, sem cookie dá 401, logout apaga o cookie, `localStorage` sem token no
navegador (Vite 5174). Etapa 2: a 6ª tentativa no mesmo minuto dá 429.
