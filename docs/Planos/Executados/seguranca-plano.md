# Segurança — Plano de implementação

> **Para agentes:** SUB-SKILL OBRIGATÓRIA: usar superpowers:subagent-driven-development ou
> superpowers:executing-plans para executar este plano tarefa por tarefa. Os passos usam caixas
> (`- [ ]`) para acompanhamento.

**Objetivo:** fechar as pendências #11, #12 e #14 no código e deixar o #13 pronto para o usuário
aplicar, sem mudar nada para quem usa o app (além de logar de novo uma vez).

**Arquitetura:** segredos saem do `appsettings.json` para o User Secrets (dev) e variáveis de
ambiente (produção); o login ganha um rate limiter nativo por IP; a sessão passa a ser um cookie
`HttpOnly` com o próprio JWT, lido pelo `JwtBearer` (o cabeçalho `Bearer` continua aceito para
Swagger e scripts). O front deixa de tocar no token.

**Stack:** ASP.NET Core 8 (`Microsoft.AspNetCore.RateLimiting`, `JwtBearerEvents`, User Secrets),
React 19 + TypeScript + axios, Postman (coleção em YAML).

**Especificação:** [seguranca.md](seguranca.md)

## Restrições globais

- Commits só quando o usuário pedir, direto na `main`, autor
  `pedro <pedrorotta2006@gmail.com>`, mensagem em português, **sem** "Co-Authored-By: Claude".
  O push é do usuário.
- **Nunca exibir segredos** (senha do banco, chave do JWT) em saída de comando, arquivo ou chat.
  Ler e mover valores só por script, com a saída descartada.
- Não alterar configuração de segurança do sistema (`postgresql.conf`, `pg_hba.conf`, firewall,
  senha do banco): fica com o usuário. Só conferências de leitura.
- Manter o padrão do código: controllers finos, comentários em português no estilo do arquivo,
  erros JSON no formato `ErroApiResponse` (`status`, `mensagem`, `traceId`, `detalhes`).
- Arquivos C# e Postman têm **CRLF**; front e `appsettings.json` têm LF. Edição em massa só por
  PowerShell (`[IO.File]::ReadAllText/WriteAllText` com `UTF8Encoding($false)`), nunca `sed -i`.
- Build com `--configuration Release` (o backend do usuário rodando trava o `bin/Debug`).
- Servidores de teste: API em **5299** (cópia do `bin/Release`, `--contentRoot` no projeto,
  `ASPNETCORE_ENVIRONMENT=Development`), Vite em **5174** (config temporária com proxy para a 5299).
  Encerrar no fim **pelo PID da porta**, nunca pelo nome do processo.
- Membros de teste com prefixo `tstnotif_`, apagados no fim por SQL com trava para os usuários reais
  (ids 1, 8, 9, 28).
- Limite de uso: conferir antes de cada tarefa e parar com qualquer limite em 95%.
- Valores fixos da especificação: cookie `sessao`; 5 tentativas por minuto por IP; mensagem
  `Muitas tentativas de login. Aguarde 1 minuto e tente de novo.`; CORS só
  `http://localhost:5173`; chave do JWT com no mínimo 32 bytes.

## Foco da revisão

Situações que a especificação implica e que nenhum teste de tarefa cobre por acaso; cada uma ganhou
uma verificação na tarefa dona:

1. **Cookie `Secure` em `http://localhost`:** o navegador precisa aceitar e reenviar o cookie pelo
   proxy do Vite; senão o app inteiro dá 401 → Tarefa 4, passo de navegador.
2. **Sessão antiga no navegador** (`token` + `membroId` no `localStorage` de antes da mudança): o
   app deve ir para o login, sem abrir o painel quebrado → Tarefa 4.
3. **6ª tentativa de login no minuto:** a tela mostra a mensagem do 429, não "Nome de usuário ou
   senha inválidos" → Tarefa 2 (API) e Tarefa 4 (tela).
4. **API sem segredos configurados** (ex.: rodada fora de Development): deve parar na hora com
   mensagem dizendo o que configurar, não com erro do Npgsql na primeira consulta → Tarefa 1.
5. **Downloads de PDF** (`responseType: 'blob'`) precisam continuar autenticados só pelo cookie →
   Tarefa 4.

---

## Mapa de arquivos

| Arquivo | Mudança |
|---|---|
| `GerenciadorContaFamiliar/GerenciadorContaFamiliar.csproj` | `UserSecretsId` |
| `GerenciadorContaFamiliar/appsettings.json` | connection string e `SecretKey` vazias; CORS só 5173 |
| `GerenciadorContaFamiliar/Program.cs` | checagem dos segredos, rate limiter, leitura do cookie no `JwtBearer` |
| `GerenciadorContaFamiliar/Services/CookieDeSessao.cs` | **novo**: nome e opções do cookie |
| `GerenciadorContaFamiliar/Controllers/AuthController.cs` | limite no login, grava o cookie, `logout` |
| `.../Application/DTOs/Responses/Auth/LoginResponse.cs` | `Token` com `[JsonIgnore]` |
| `frontend/src/services/api.ts` | sai o interceptor de requisição; 401 confere `membroId` |
| `frontend/src/services/auth.ts` | `logout()` |
| `frontend/src/types/auth.ts` | sai `token` |
| `frontend/src/context/authContextDef.ts`, `AuthContext.tsx` | `token` → `logado`; sem token no `localStorage` |
| `frontend/src/components/ProtectedRoute.tsx` | usa `logado` |
| `postman/collections/GerenciadorFamiliar/*.request.yaml` | sem `Bearer {{token}}`; login não salva token |
| `docs/pendencias.md`, `docs/estado-do-projeto.md` | pendências resolvidas/novas e dicas de ambiente |

---

### Tarefa 1 (Etapa 1, #11): segredos fora do `appsettings.json`

**Arquivos:**
- Modificar: `GerenciadorContaFamiliar/GerenciadorContaFamiliar.csproj` (via `dotnet user-secrets init`)
- Modificar: `GerenciadorContaFamiliar/appsettings.json` (só por script)
- Modificar: `GerenciadorContaFamiliar/Program.cs` (bloco "AUTENTICAÇÃO JWT" e "BANCO DE DADOS")

**Interfaces:**
- Produz: segredos `ConnectionStrings:DefaultConnection` e `JwtSettings:SecretKey` no User Secrets
  do projeto; a API para na inicialização se faltar algum.

- [ ] **Passo 1: Teste que falha (API sem segredos tem de parar com mensagem clara)**

Hoje a API sobe mesmo sem connection string. Com o `bin/Release` atual copiado para o scratchpad e
uma variável vazia sobrescrevendo a connection string:

```bash
cd "$SCRATCH/api-teste" && ASPNETCORE_ENVIRONMENT=Development ConnectionStrings__DefaultConnection="" \
  timeout 20 dotnet GerenciadorContaFamiliar.dll --urls http://localhost:5299 \
  --contentRoot /c/Users/pedro/Desktop/GestorFinanceiroCSharp/GerenciadorContaFamiliar 2>&1 | head -5
```

Esperado agora: a API sobe ("Now listening on"), ou seja, **falha** o critério.

- [ ] **Passo 2: Criar o User Secrets e copiar a connection string atual (sem exibir)**

```powershell
cd C:\Users\pedro\Desktop\GestorFinanceiroCSharp\GerenciadorContaFamiliar
dotnet user-secrets init | Out-Null
$cfg = Get-Content appsettings.json -Raw | ConvertFrom-Json
dotnet user-secrets set "ConnectionStrings:DefaultConnection" $cfg.ConnectionStrings.DefaultConnection | Out-Null
```

- [ ] **Passo 3: Gerar a chave nova do JWT direto no User Secrets (sem exibir)**

```powershell
$bytes = New-Object byte[] 64
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
dotnet user-secrets set "JwtSettings:SecretKey" ([Convert]::ToBase64String($bytes)) | Out-Null
dotnet user-secrets list | ForEach-Object { ($_ -split ' = ')[0] }   # só os nomes
```

Esperado: `ConnectionStrings:DefaultConnection` e `JwtSettings:SecretKey`.

- [ ] **Passo 4: Esvaziar os segredos e enxugar o CORS no `appsettings.json` (por script, LF)**

```powershell
$p = 'C:\Users\pedro\Desktop\GestorFinanceiroCSharp\GerenciadorContaFamiliar\appsettings.json'
$t = [IO.File]::ReadAllText($p)
$t = $t -replace '"DefaultConnection":\s*"[^"]*"', '"DefaultConnection": ""'
$t = $t -replace '"SecretKey":\s*"[^"]*"', '"SecretKey": ""'
$t = $t -replace '"http://localhost:3000",\s*', '' -replace '"http://localhost:4200",\s*', ''
[IO.File]::WriteAllText($p, $t, (New-Object Text.UTF8Encoding($false)))
```

Conferir com `git diff GerenciadorContaFamiliar/appsettings.json` **só depois** do passo: o diff
mostra as linhas removidas, então filtrar a saída (`git diff --stat` e
`git diff ... | grep '^+'`), nunca o diff completo.

- [ ] **Passo 5: Checagens na inicialização (`Program.cs`)**

Substituir a leitura da chave no bloco "AUTENTICAÇÃO JWT":

```csharp
// Segredos ficam fora do git: User Secrets no desenvolvimento, variáveis de ambiente em produção.
var secretKey = builder.Configuration["JwtSettings:SecretKey"];
if (string.IsNullOrWhiteSpace(secretKey))
    throw new InvalidOperationException(
        "JwtSettings:SecretKey não configurado. No desenvolvimento: " +
        "dotnet user-secrets set \"JwtSettings:SecretKey\" \"<chave>\" (pasta GerenciadorContaFamiliar).");
if (Encoding.UTF8.GetByteCount(secretKey) < 32)
    throw new InvalidOperationException("JwtSettings:SecretKey precisa ter pelo menos 32 bytes.");
```

E no bloco "BANCO DE DADOS":

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection não configurada. No desenvolvimento: " +
        "dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"Host=localhost;...\" " +
        "(pasta GerenciadorContaFamiliar).");

builder.Services.AddDbContext<GerenciadorContaFamiliarDbContext>(options =>
    options.UseNpgsql(connectionString));
```

- [ ] **Passo 6: Build e teste passando**

```bash
cd /c/Users/pedro/Desktop/GestorFinanceiroCSharp && dotnet build GerenciadorContaFamiliar.sln --configuration Release 2>&1 | tail -3
```

Copiar de novo o `bin/Release` para `$SCRATCH/api-teste` e repetir o passo 1. Esperado: a API
**não sobe** e mostra `ConnectionStrings:DefaultConnection não configurada...`.
Depois, sem a variável vazia (lendo o User Secrets):

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet GerenciadorContaFamiliar.dll --urls http://localhost:5299 \
  --contentRoot /c/Users/pedro/Desktop/GestorFinanceiroCSharp/GerenciadorContaFamiliar   # em segundo plano
curl -s -o /dev/null -w "%{http_code}\n" -X POST http://localhost:5299/api/Auth/login \
  -H "Content-Type: application/json" -d '{"nomeDeUsuario":"nao_existe_x","senha":"x"}'
```

Esperado: `400` (a API sobe, conecta no banco e responde "Nome de usuário ou senha inválidos").
Encerrar a API pelo PID da porta 5299.

- [ ] **Passo 7: Instruções ao usuário (senha do banco)**

Avisar no chat: trocar a senha do `postgres` no pgAdmin (Login/Group Roles → `postgres` →
Definition → Password) e em seguida, na pasta `GerenciadorContaFamiliar`:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=GerenciadorFamiliar;Username=postgres;Password=<nova>"
```

Reiniciar o backend depois. A chave nova do JWT derruba as sessões abertas.

- [ ] **Passo 8: Análise de padrão/qualidade e parada da etapa**

Conferir: mensagens em português, comentários no estilo do `Program.cs`, `appsettings.json` sem
segredos (`git grep -n "Password=" -- GerenciadorContaFamiliar/appsettings.json` vazio). Relatar ao
usuário; commit só se ele pedir:
`Tira os segredos do appsettings.json (User Secrets) e confere na inicialização`.

---

### Tarefa 2 (Etapa 2, #12): limite de tentativas no login

**Arquivos:**
- Modificar: `GerenciadorContaFamiliar/Program.cs` (serviços + pipeline)
- Modificar: `GerenciadorContaFamiliar/Controllers/AuthController.cs`

**Interfaces:**
- Produz: política de rate limiting `"login"` (constante `AuthController.PoliticaLogin`), 429 com
  `ErroApiResponse`.

- [ ] **Passo 1: Teste que falha**

Com a API de teste no ar (5299), 6 logins seguidos:

```bash
for i in 1 2 3 4 5 6; do curl -s -o /dev/null -w "%{http_code} " -X POST http://localhost:5299/api/Auth/login \
  -H "Content-Type: application/json" -d '{"nomeDeUsuario":"nao_existe_x","senha":"x"}'; done; echo
```

Esperado agora: `400 400 400 400 400 400` (**falha**: o 6º deveria ser 429).

- [ ] **Passo 2: Registrar o rate limiter (`Program.cs`, depois do bloco de CORS)**

Usings novos no topo, no grupo "Framework":

```csharp
using System.Threading.RateLimiting;
using GerenciadorContaFamiliar.Controllers;
using Microsoft.AspNetCore.RateLimiting;
```

Bloco:

```csharp
// ================================================================
// LIMITE DE TENTATIVAS NO LOGIN
// 5 por minuto por IP. Atrás de um proxy (inclusive o do Vite) todos chegam com o
// mesmo IP: em produção é preciso configurar ForwardedHeaders.
// ================================================================

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(AuthController.PoliticaLogin, contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // Mesmo formato de erro do ExceptionHandlingMiddleware
    options.OnRejected = async (contexto, ct) =>
    {
        var resposta = contexto.HttpContext.Response;
        resposta.ContentType = "application/json; charset=utf-8";
        await resposta.WriteAsJsonAsync(new ErroApiResponse(
            Status: StatusCodes.Status429TooManyRequests,
            Mensagem: "Muitas tentativas de login. Aguarde 1 minuto e tente de novo.",
            TraceId: contexto.HttpContext.TraceIdentifier,
            Detalhes: null), ct);
    };
});
```

(`ErroApiResponse` está em `GerenciadorContaFamiliar.Middleware`, já importado; o
`WriteAsJsonAsync` usa camelCase por padrão.)

No pipeline, logo depois de `app.UseCors("PermitirFrontend");`:

```csharp
// Limite de tentativas (política "login" no AuthController)
app.UseRateLimiter();
```

- [ ] **Passo 3: Aplicar a política no `AuthController`**

```csharp
using Microsoft.AspNetCore.RateLimiting;
...
public class AuthController : BaseApiController
{
    /// <summary>Política de limite de tentativas do login (registrada no Program.cs).</summary>
    public const string PoliticaLogin = "login";
    ...
    [HttpPost("login")]
    [AllowAnonymous]  // Login NÃO precisa de token (é o endpoint que GERA o token)
    [EnableRateLimiting(PoliticaLogin)]  // 5 tentativas por minuto por IP
```

- [ ] **Passo 4: Build e teste passando**

Build Release, nova cópia para a API de teste, aguardar 1 minuto desde o último teste e repetir o
passo 1. Esperado: `400 400 400 400 400 429`. Conferir o corpo do 429:

```bash
curl -s -X POST http://localhost:5299/api/Auth/login -H "Content-Type: application/json" \
  -d '{"nomeDeUsuario":"nao_existe_x","senha":"x"}'
```

Esperado: `{"status":429,"mensagem":"Muitas tentativas de login. Aguarde 1 minuto e tente de novo.",...}`.
Conferir também que outra rota anônima não é limitada (7 × `GET /swagger/v1/swagger.json` → 200).

- [ ] **Passo 5: Análise de padrão/qualidade e parada da etapa**

Registrar em `docs/pendencias.md` a pendência nova: "Rate limit atrás de proxy — todos chegam
como `127.0.0.1`; configurar `ForwardedHeaders` em produção". Relatar; commit só se pedido:
`Limita as tentativas de login a 5 por minuto por IP`.

---

### Tarefa 3 (Etapa 3, #14): cookie de sessão no backend

**Arquivos:**
- Criar: `GerenciadorContaFamiliar/Services/CookieDeSessao.cs`
- Modificar: `GerenciadorContaFamiliar/Controllers/AuthController.cs`
- Modificar: `GerenciadorContaFamiliar/Program.cs` (`AddJwtBearer`)
- Modificar: `GerenciadorContaFamiliar/GerenciadorContaFamiliar.Application/DTOs/Responses/Auth/LoginResponse.cs`

**Interfaces:**
- Produz: `POST api/Auth/login` → 200 com `{ membroId, nome, nomeDeUsuario, email, expiraEm }` e
  `Set-Cookie: sessao=...; HttpOnly; Secure; SameSite=Strict; Path=/api`;
  `POST api/Auth/logout` → 204 e cookie apagado.

- [ ] **Passo 1: Teste que falha**

```bash
J="$SCRATCH/cookies.txt"; rm -f "$J"
curl -s -c "$J" -X POST http://localhost:5299/api/Auth/login -H "Content-Type: application/json" \
  -d '{"nomeDeUsuario":"tstnotif_seg1","senha":"Teste@123"}' | grep -o '"token"' ; grep -c sessao "$J"
```

(Antes, criar o membro `tstnotif_seg1` com `POST api/Membro` —
`{"nome":"Teste Seg","nomeDeUsuario":"tstnotif_seg1","email":"tstnotif_seg1@teste.local","senha":"Teste@123"}`.)
Esperado agora: aparece `"token"` e o cookie `sessao` não existe (`0`) → **falha**.

- [ ] **Passo 2: `CookieDeSessao.cs` (CRLF, sem BOM)**

```csharp
namespace GerenciadorContaFamiliar.Services;

/// <summary>
/// Cookie que guarda o JWT da sessão. HttpOnly: o JavaScript do front não consegue lê-lo,
/// então uma falha de XSS não rouba a sessão. SameSite=Strict: não vai em requisições de outros sites.
/// </summary>
public static class CookieDeSessao
{
    public const string Nome = "sessao";

    public static CookieOptions Opcoes(DateTime expiraEm) => new()
    {
        HttpOnly = true,
        Secure = true,              // localhost conta como seguro mesmo em http
        SameSite = SameSiteMode.Strict,
        Path = "/api",
        Expires = expiraEm
    };
}
```

- [ ] **Passo 3: `LoginResponse` sem o token no JSON**

```csharp
using System.Text.Json.Serialization;
...
    /// <summary>Vai só no cookie de sessão (HttpOnly), nunca no corpo da resposta.</summary>
    [JsonIgnore]
    public string Token { get; set; } = null!;
```

- [ ] **Passo 4: `AuthController` grava o cookie e ganha o logout**

```csharp
using GerenciadorContaFamiliar.Services;
...
    /// <summary>
    /// Realiza login. Grava o token JWT no cookie de sessão (HttpOnly).
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]  // Login NÃO precisa de token (é o endpoint que GERA o token)
    [EnableRateLimiting(PoliticaLogin)]  // 5 tentativas por minuto por IP
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct = default)
    {
        var response = await _loginUseCase.ExecutarAsync(request, ct);
        Response.Cookies.Append(CookieDeSessao.Nome, response.Token, CookieDeSessao.Opcoes(response.ExpiraEm));
        return Ok(response);
    }

    /// <summary>
    /// Encerra a sessão apagando o cookie. Anônimo: funciona mesmo com a sessão já expirada.
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(CookieDeSessao.Nome, CookieDeSessao.Opcoes(DateTime.UtcNow));
        return NoContent();
    }
```

- [ ] **Passo 5: `JwtBearer` lê o cookie (`Program.cs`, dentro de `AddJwtBearer`)**

Depois de `TokenValidationParameters`:

```csharp
    // O front manda o token no cookie de sessão; o cabeçalho Bearer continua valendo
    // para o Swagger e scripts (usado quando não há cookie).
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = contexto =>
        {
            if (contexto.Request.Cookies.TryGetValue(CookieDeSessao.Nome, out var token))
                contexto.Token = token;
            return Task.CompletedTask;
        }
    };
```

(`GerenciadorContaFamiliar.Services` já está nos usings.)

- [ ] **Passo 6: Build e testes passando**

Build Release, nova cópia, API de teste no ar. Repetir o passo 1: esperado sem `"token"` e `1`
linha com `sessao` no cookie jar (`#HttpOnly_localhost ... /api ... sessao`). Depois:

```bash
curl -s -o /dev/null -w "com cookie: %{http_code}\n" -b "$J" http://localhost:5299/api/Conta
curl -s -o /dev/null -w "sem cookie: %{http_code}\n" http://localhost:5299/api/Conta
curl -s -o /dev/null -w "logout: %{http_code}\n" -b "$J" -c "$J" -X POST http://localhost:5299/api/Auth/logout
grep -c "sessao" "$J"
curl -s -o /dev/null -w "depois do logout: %{http_code}\n" -b "$J" http://localhost:5299/api/Conta
```

Esperado: `200`, `401`, `204`, `0`, `401`. Cabeçalho ainda aceito: logar de novo, extrair o valor
do cookie para uma variável (sem exibir) e chamar `GET api/Conta` com
`-H "Authorization: Bearer $T"` e sem `-b` → `200`. Atenção ao limite de 5 logins por minuto
entre os testes.

---

### Tarefa 4 (Etapa 3, #14): front sem token

**Arquivos:**
- Modificar: `frontend/src/services/api.ts`, `frontend/src/services/auth.ts`,
  `frontend/src/types/auth.ts`, `frontend/src/context/authContextDef.ts`,
  `frontend/src/context/AuthContext.tsx`, `frontend/src/components/ProtectedRoute.tsx`

**Interfaces:**
- Consome: `POST api/Auth/login` sem `token` no corpo; `POST api/Auth/logout` (Tarefa 3).
- Produz: `useAuth()` com `logado: boolean` no lugar de `token`.

- [ ] **Passo 1: `types/auth.ts` — tirar `token` do `LoginResponse`**

```ts
export interface LoginResponse {
  membroId: number
  nome: string
  nomeDeUsuario: string
  email: string
  expiraEm: string
}
```

Rodar `cd frontend && npx tsc -b`. Esperado: **falha** em `AuthContext.tsx` (`data.token`), o que
mostra onde o token ainda é usado.

- [ ] **Passo 2: `services/api.ts`**

Remover o `api.interceptors.request.use(...)` inteiro. No interceptor de resposta, trocar a
condição e o comentário:

```ts
    // 401 sem corpo = o JwtBearer recusou o cookie de sessão (expirado ou inválido).
    ...
    if (axios.isAxiosError(erro) && erro.response?.status === 401 && localStorage.getItem('membroId')) {
```

Comentário do topo do arquivo, se houver menção ao token, passa a dizer que a sessão vai no cookie
`HttpOnly` enviado pelo navegador.

- [ ] **Passo 3: `services/auth.ts`**

```ts
/**
 * Serviço de API para Autenticação.
 * A sessão fica num cookie HttpOnly gravado pela API: o front nunca vê o token.
 */

import api from './api'
import type { LoginResponse } from '../types/auth'

/** POST api/Auth/login — autentica; a API grava o cookie de sessão */
export async function login(
  nomeDeUsuario: string,
  senha: string,
): Promise<LoginResponse> {
  const { data } = await api.post<LoginResponse>('/Auth/login', {
    nomeDeUsuario,
    senha,
  })
  return data
}

/** POST api/Auth/logout — apaga o cookie de sessão */
export async function logout(): Promise<void> {
  await api.post('/Auth/logout')
}
```

- [ ] **Passo 4: `authContextDef.ts`**

```ts
export interface AuthContextType {
  /** Há sessão aberta (o token fica no cookie HttpOnly, fora do alcance do JavaScript) */
  logado: boolean
  membroId: number | null
  nome: string | null
  /** E-mail do membro logado */
  email: string | null
  nomeDeUsuario: string | null
  login: (nomeDeUsuario: string, senha: string) => Promise<void>
  logout: () => void
}
```

- [ ] **Passo 5: `AuthContext.tsx`**

- Apagar `emailDoToken` e o `useState` de `token`.
- Antes do componente, a limpeza da sessão antiga (lida uma vez, no carregamento do módulo):

```ts
// Chaves da sessão no localStorage (nenhuma é segredo: o token fica no cookie HttpOnly)
const CHAVES_DA_SESSAO = ['membroId', 'nome', 'email', 'nomeDeUsuario', 'expiraEm']

// Sessão de antes do cookie: o token antigo não vale mais (chave do JWT trocada).
// Limpa tudo para a pessoa logar de novo.
if (localStorage.getItem('token')) {
  localStorage.removeItem('token')
  CHAVES_DA_SESSAO.forEach((chave) => localStorage.removeItem(chave))
}
```

- `email` passa a ser `useState(() => localStorage.getItem('email'))`.
- `const logado = membroId !== null` e o efeito do timer usa `logado` no lugar de `token`
  (`if (!logado || !expiraEm) return`, dependências `[logado, expiraEm]`).
- `login`: sem `localStorage.setItem('token', ...)` e sem `setToken`.
- `logout`:

```ts
  const logout = () => {
    // Apaga o cookie na API; sem rede, sai do mesmo jeito (o cookie expira sozinho)
    sairDaApi().catch(() => {})
    CHAVES_DA_SESSAO.forEach((chave) => localStorage.removeItem(chave))
    setMembroId(null)
    ...demais setters como hoje, sem setToken...
  }
```

  com `import { login as autenticar, logout as sairDaApi } from '../services/auth'`.
- Provider: `value={{ logado, membroId, nome, email, nomeDeUsuario, login, logout }}` e
  `<SessaoExpiradaModal aberto={sessaoExpirada && logado} aoFechar={logout} />`.

- [ ] **Passo 6: `ProtectedRoute.tsx`**

```tsx
  const { logado } = useAuth()
  if (!logado) return <Navigate to="/login" replace />
```

- [ ] **Passo 7: Checagens estáticas**

```bash
cd /c/Users/pedro/Desktop/GestorFinanceiroCSharp/frontend && npx tsc -b && npx oxlint src && grep -rn "token" src | grep -v -i "csrf"
```

Esperado: `tsc` e `oxlint` sem erros; o `grep` só acha comentários (ex.: `SessaoExpiradaModal`
"login (token JWT)", que pode ficar ou virar "login").

- [ ] **Passo 8: Teste no navegador (Vite 5174 → API 5299)**

Subir o Vite de teste (config temporária com proxy `/api` → 5299) e, no navegador embutido:
1. Login com `tstnotif_seg1` → painel abre. `localStorage` (via `javascript_tool`) sem `token`;
   `document.cookie` **não** mostra `sessao` (é HttpOnly). Requisições de `/api` com 200.
2. Exportar um PDF de transações → download funciona (Foco 5).
3. "Sair da conta" → volta ao login; `POST /api/Auth/logout` com 204; navegar para `/dashboard` →
   redireciona para `/login`.
4. Sessão antiga (Foco 2): `localStorage.setItem('token','x'); localStorage.setItem('membroId','999')`,
   recarregar `/dashboard` → vai para `/login` e o `localStorage` fica limpo.
5. Senha errada 6 vezes (Foco 3) → a 6ª mostra "Muitas tentativas de login. Aguarde 1 minuto e
   tente de novo.".

---

### Tarefa 5 (Etapa 3, #14): Postman com cookie

**Arquivos:**
- Modificar: `postman/collections/GerenciadorFamiliar/*.request.yaml` (CRLF)

- [ ] **Passo 1: Tirar o `auth` bearer de todas as requisições (PowerShell)**

```powershell
$dir = 'C:\Users\pedro\Desktop\GestorFinanceiroCSharp\postman\collections\GerenciadorFamiliar'
$bloco = "auth:`r`n  type: bearer`r`n  credentials:`r`n    token: `"{{token}}`"`r`n"
Get-ChildItem $dir -Filter *.request.yaml | ForEach-Object {
  $t = [IO.File]::ReadAllText($_.FullName)
  if ($t.Contains($bloco)) {
    [IO.File]::WriteAllText($_.FullName, $t.Replace($bloco, ''), (New-Object Text.UTF8Encoding($false)))
  }
}
```

Conferir: `grep -rl "bearer" postman` vazio (antes: 26 arquivos).

- [ ] **Passo 2: Script do login**

Em `Login.request.yaml`, trocar o `code` do `afterResponse` por (mantendo o formato de uma linha
com `\r\n`):

```js
const r = pm.response.json();
if (pm.response.code === 200) {
  pm.collectionVariables.set('membroId', r.membroId);
  console.log('Login feito: o cookie de sessão fica com o Postman. membroId salvo!');
} else {
  console.log('Falha no login:', pm.response.text());
}
```

- [ ] **Passo 3: Análise de padrão/qualidade e parada da etapa**

Revisar as Tarefas 3–5 juntas (padrão, comentários, nada do token sobrando). Relatar; commit só se
pedido: `Sessão em cookie HttpOnly: login grava o cookie, logout e front sem token`.

---

### Tarefa 6: documentação, conferência do #13 e limpeza

**Arquivos:**
- Modificar: `docs/pendencias.md`, `docs/estado-do-projeto.md`, `docs/seguranca.md` (situação)
- Memória: `acesso-banco-local.md`, `host-banco-localhost.md` (connection string agora no User Secrets)

- [ ] **Passo 1: Limpeza dos testes**

Apagar os membros `tstnotif_%` por SQL (mini app Npgsql no scratchpad, lendo a connection string de
`%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`), com trava
`WHERE "NomeDeUsuario" LIKE 'tstnotif\_%' AND "Id" NOT IN (1, 8, 9, 28)`. Encerrar API 5299 e Vite
5174 pelo PID da porta.

- [ ] **Passo 2: `pendencias.md`**

Mover #11, #12 e #14 para "Resolvidas" com o commit (ou "sem commit" se o usuário não tiver pedido).
#13 fica aberta com "aguarda o usuário aplicar (passos em `seguranca.md`)". Nova pendência de
`ForwardedHeaders` (da Tarefa 2), se ainda não estiver lá.

- [ ] **Passo 3: `estado-do-projeto.md`**

Em "Ambiente": segredos no User Secrets (comando para ver só os nomes:
`dotnet user-secrets list`); API de teste precisa de `ASPNETCORE_ENVIRONMENT=Development`; o mini
app Npgsql lê o `secrets.json`; teste com curl usa cookie jar (`-c`/`-b`) e respeita 5 logins/min.
Em "Estado atual" e "Histórico": a segurança feita.

- [ ] **Passo 4: Conferência do #13 (quando o usuário avisar que aplicou)**

```powershell
Get-NetTCPConnection -LocalPort 5432 -State Listen | Select-Object LocalAddress
Get-NetFirewallPortFilter -Protocol TCP | Where-Object LocalPort -eq 5432 | Get-NetFirewallRule |
  Where-Object { $_.Enabled -eq 'True' -and $_.Direction -eq 'Inbound' } | Select-Object DisplayName, Action
```

Esperado: só `127.0.0.1` e `::1`; nenhuma regra de entrada liberando a 5432. Então mover a #13
para "Resolvidas".
