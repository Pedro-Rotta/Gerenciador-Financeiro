# Esqueceu a senha e alterar senha — Plano de implementação

> Execução nativa (executing-plans), tarefa por tarefa, com teste que falha antes de cada mudança.

**Objetivo:** "Esqueceu a senha" com código por e-mail, "Alterar senha" logado e sessões derrubadas
ao trocar a senha.

**Arquitetura:** `Membro.VersaoSessao` vai no JWT e é conferida a cada requisição
(`OnTokenValidated`); `Membro.TrocarSenha` sobe a versão. Códigos de recuperação numa tabela própria
(só o hash). E-mail atrás de `IEnvioDeEmail`: MailKit (SMTP) ou console no desenvolvimento.

**Stack:** ASP.NET Core 8, EF Core 8, MailKit (NuGet, MIT), React 19 + TypeScript.

**Especificação:** [recuperar-senha.md](recuperar-senha.md)

## Restrições globais

- Commits só quando o usuário pedir; autor `pedro <pedrorotta2006@gmail.com>`, sem coautoria.
- **Migração no banco local só com confirmação do usuário.** Segredos (SMTP) gravados **pelo
  usuário, no terminal dele** (o sandbox do Claude não grava no APPDATA real).
- Senha nova: 6 a 100 caracteres, confirmação igual (mesmas mensagens do cadastro).
- Código: 6 dígitos, `RandomNumberGenerator`, guardado como hash (BCrypt via `IPasswordHasher`),
  15 min, 5 tentativas, um pedido novo invalida o anterior.
- Limite: política `recuperacao`, 5 pedidos a cada 10 minutos por IP, nos 3 endpoints.
- Mensagens fixas: "Se a conta existir, enviamos um código para o e-mail cadastrado.",
  "Código inválido ou expirado.", "Senha atual incorreta.".
- Padrão do código: use cases + interfaces, controllers finos, CRLF nos C# novos.

## Foco da revisão

1. **Pedir código para conta inexistente ou inativa:** mesma resposta e mesmo código HTTP da conta
   real (sem revelar cadastro) → Tarefa 3.
2. **Token antigo sem a claim de versão** (sessões de antes da mudança): recusado com 401, sem 500 →
   Tarefa 1.
3. **Alterar senha logado:** o navegador atual continua logado (cookie novo) e outro navegador cai →
   Tarefa 2.
4. **Código usado duas vezes / pedido novo:** o código antigo deixa de valer → Tarefa 3.
5. **Identificador com espaços ou maiúsculas no e-mail:** encontra a conta como o login encontra
   (usuário diferencia maiúsculas; e-mail não) → Tarefa 3.

---

## Etapa 1

### Tarefa 1: versão da sessão

- `Membro`: `public int VersaoSessao { get; private set; }`; `TrocarSenha(string novaSenha,
  IPasswordHasher hasher)` → valida 6–100, `DefinirSenha`, `VersaoSessao++`.
- `ITokenService.GerarToken(int membroId, string email, int versaoSessao)` e claim `versao_sessao`
  no `JwtTokenService`; `LoginUseCase` passa `membro.VersaoSessao`.
- `Program.cs`, `JwtBearerEvents.OnTokenValidated`: lê `versao_sessao` (ausente → `Fail`) e o membro
  pelo id (`IMembroRepository.ObterPorIdAsync`); versão diferente ou membro inexistente/inativo →
  `contexto.Fail("Sessão encerrada.")`.
- Migração `RecuperacaoDeSenha` (junto com a tabela da Tarefa 3).
- **Testes:** RED — token antigo (gerado antes) ainda passa; GREEN — token sem a claim → 401;
  login novo → 200.

### Tarefa 2: alterar senha logado

- `AlterarSenhaRequest { SenhaAtual, NovaSenha, ConfirmacaoSenha, MembroId }` (DataAnnotations como
  o cadastro); `IAlterarSenhaUseCase` / `AlterarSenhaUseCase`: confere a atual (`VerificarSenha`,
  senão 400 "Senha atual incorreta."), `TrocarSenha`, salva e devolve o token novo (`LoginResponse`).
- `MembroController`: `PUT api/Membro/senha` com `[EnableRateLimiting("recuperacao")]`; grava o
  cookie novo (`CookieDeSessao`) e devolve 204.
- Política `recuperacao` no `Program.cs` (janela fixa 10 min, 5 pedidos, mesma resposta 429).
- **Testes:** dois cookie jars logados; alterar no jar A → A segue 200 em `GET api/Conta`, B dá 401;
  senha atual errada → 400; login com a nova → 200, com a antiga → 400; 6º pedido → 429.

## Etapa 2

### Tarefa 3: código de recuperação e endpoints

- Entidade `CodigoRecuperacaoSenha` (`MembroId`, `CodigoHash`, `ExpiraEm`, `Tentativas`, `Usado`,
  `CriadoEm`; `EstaValido(agora)`, `RegistrarErro()`, `MarcarUsado()`); `DbSet` +
  `ICodigoRecuperacaoSenhaRepository` (`ObterAtivoDoMembroAsync`, `InvalidarDoMembroAsync`,
  `AdicionarAsync`); `CodigoHash` em `CamposSensiveis` da auditoria.
- `IEnvioDeEmail.EnviarAsync(para, assunto, texto)`; `SmtpEnvioDeEmail` (MailKit, `Email:*`) e
  `ConsoleEnvioDeEmail` (log `Warning` com o texto); `Program.cs` escolhe: `Email:Host` vazio →
  console em Development, erro na inicialização fora dele.
- `IEsqueciSenhaUseCase` (`POST api/Auth/esqueci-senha`, `{ identificador }`): acha por usuário
  (exato, sem espaços nas pontas) ou e-mail (`ObterPorEmailAsync`); conta ativa → invalida códigos
  anteriores, gera e guarda, envia; sempre a mesma resposta 200.
- `IRedefinirSenhaUseCase` (`POST api/Auth/redefinir-senha`): acha o membro e o código ativo; inválido
  → 400 "Código inválido ou expirado." (erro conta tentativa); certo → `TrocarSenha`, `MarcarUsado`;
  200 com mensagem.
- **Testes** (código lido do `api.log`): inexistente e real → mesma resposta; código certo troca a
  senha e derruba a sessão aberta; reuso → 400; pedido novo invalida o anterior; 5 erros invalidam;
  e-mail em maiúsculas acha a conta; 6º pedido → 429.

## Etapa 3

### Tarefa 4: telas

- `Login.tsx`: embaixo de "Não tem conta? Cadastre-se", `Esqueceu a senha? <Link to="/recuperar-senha">Nova senha</Link>`;
  mensagem de sucesso vinda do `state` da navegação.
- `RecuperarSenha.tsx` (rota pública `/recuperar-senha`, visual `auth-page`/`auth-card`): passo 1
  (identificador → "Enviar código"), passo 2 (código, nova, confirmação → "Salvar nova senha";
  "Reenviar código"); sucesso → `/login` com "Senha alterada. Entre com a nova senha.".
- `services/auth.ts`: `pedirCodigoSenha`, `redefinirSenha`; `services/membros.ts`: `alterarSenha`.
- `Configuracoes.tsx`, aba Conta: bloco "Alterar senha" embaixo de Telefone (funcional); sai o bloco
  desativado da aba Segurança.
- **Testes:** `tsc`, `oxlint`; navegador: link no login, fluxo completo com código do log, mensagens
  de erro, "Alterar senha" nas Configurações e sessão do navegador mantida.

### Tarefa 5: docs, limpeza e revisão final

- `pendencias.md` (#3 continua até o e-mail real), `estado-do-projeto.md`, situação da
  especificação; membros de teste apagados; revisão final por agente revisor.
