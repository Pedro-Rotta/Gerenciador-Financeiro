# Esqueceu a senha e alterar senha

**Situação:** implementada e testada em 14/10/2026 (API de teste + navegador), sem commit. Migração
`RecuperacaoDeSenha` aplicada no banco local com autorização. Falta: o usuário configurar o e-mail
real (pendência #3). Revisão final por agente revisor feita em 14/10: 3 pontos importantes
corrigidos e testados (limite por conta, tentativas atômicas, redefinir com sessão aberta).

**Pedido (14/10/2026):** link "Esqueceu a senha? Nova senha" embaixo de "Não tem conta? Cadastre-se"
no login, e o método para alterar a senha.

**Decisões do usuário:** a prova de dono é um **código por e-mail**; trocar a senha **derruba as
sessões abertas**; também existe o "Alterar senha" para quem está logado, na aba **Conta** das
Configurações, **embaixo de Telefone**.

## O que já existia

- `Membro.DefinirSenha` (hash BCrypt) e `VerificarSenha`; cadastro exige senha de 6 a 100 caracteres
  com confirmação.
- Sessão: JWT de 60 min no cookie `sessao` (HttpOnly); não há como revogar um token antes de expirar.
- Login limitado a 5 tentativas/minuto por IP. Nenhum envio de e-mail no projeto.
- Configurações: aba Conta com campos desativados (pré-visualização); aba Segurança com "Alterar
  senha" desativado.

## Regras

### Esqueceu a senha (deslogado)

1. **Pedir o código** — `POST api/Auth/esqueci-senha` `{ identificador }` (nome de usuário ou e-mail):
   - Resposta **sempre igual** (200, "Se a conta existir, enviamos um código para o e-mail
     cadastrado."), exista ou não a conta: não revela quem tem cadastro.
   - Conta ativa encontrada: gera um código de **6 dígitos** (aleatório criptográfico), guarda só o
     **hash**, válido por **15 minutos**, e manda para o e-mail cadastrado. Um pedido novo invalida
     o código anterior.
2. **Redefinir** — `POST api/Auth/redefinir-senha` `{ identificador, codigo, novaSenha,
   confirmacaoSenha }`:
   - Código certo, dentro do prazo e não usado → troca a senha, marca o código como usado e
     **derruba todas as sessões**. A pessoa entra de novo pelo login.
   - Código errado → 400 "Código inválido ou expirado." e conta 1 tentativa; com **5 erros** o
     código deixa de valer (precisa pedir outro). Expirado, usado ou conta inexistente → a mesma
     mensagem.
   - Senha nova: mesmas regras do cadastro (6 a 100 caracteres, confirmação igual).
3. **Limite:** os dois endpoints têm um limite próprio de **5 pedidos a cada 10 minutos por IP**
   (429 com mensagem), contados separadamente em cada endpoint, além do limite de tentativas por código.
4. **Limite por conta** (revisão final): no máximo **1 código por minuto e 5 por hora** para a mesma
   conta; acima disso a resposta é a mesma e nenhum código é gerado nem enviado (protege o e-mail da
   pessoa e a cota de envio). As tentativas são gastas no banco de forma atômica, então palpites em
   paralelo não passam das 5. O e-mail sai sempre com TLS (465 cifrado na conexão; nas outras
   portas, STARTTLS obrigatório) e desiste em 15 s.

### Alterar senha (logado)

- `PUT api/Membro/senha` `{ senhaAtual, novaSenha, confirmacaoSenha }`: senha atual errada → 400
  "Senha atual incorreta."; mesmas regras de senha nova; mesmo limite de 5 a cada 10 minutos.
- Troca a senha e derruba as **outras** sessões; o navegador atual recebe um cookie novo e continua
  logado.

### Derrubar sessões

- Campo `VersaoSessao` no membro (começa em 0), gravado no JWT. Trocar a senha soma 1.
- A cada requisição autenticada, a API confere a versão do token com a do membro (1 consulta pela
  chave primária); diferente → 401, e o front mostra o aviso de sessão expirada.

### E-mail

- Envio por **SMTP** com a biblioteca **MailKit** (MIT), atrás de uma interface (`IEnvioDeEmail`).
- Configuração no User Secrets (gravada **pelo usuário, no terminal dele**): `Email:Host`,
  `Email:Porta`, `Email:Usuario`, `Email:Senha`, `Email:Remetente`. Ex.: Gmail, porta 587, com
  "senha de app" da conta Google.
- **Desenvolvimento sem e-mail configurado:** o código vai para o console da API (log), para dar
  para testar. Fora de Development, a API não sobe sem a configuração de e-mail.
- Texto do e-mail: "Seu código para criar uma nova senha no Gerenciador Familiar é 123456. Ele vale
  por 15 minutos. Se não foi você, ignore este e-mail."

## Telas

- **Login:** embaixo de "Não tem conta? Cadastre-se", o link "Esqueceu a senha? **Nova senha**",
  que abre `/recuperar-senha` (tela pública, no mesmo visual do login).
- **`/recuperar-senha`**, em 2 passos:
  1. "Nome de usuário ou e-mail" + "Enviar código" → mostra a mensagem genérica e passa ao passo 2.
  2. "Código" (6 dígitos) + "Nova senha" + "Confirmar nova senha" + "Salvar nova senha"; link
     "Reenviar código". Sucesso → volta ao login com "Senha alterada. Entre com a nova senha.".
- **Configurações → Conta:** bloco "Alterar senha" embaixo do campo Telefone (senha atual, nova,
  confirmação, botão "Atualizar senha"), funcionando; os outros campos da aba continuam em
  pré-visualização. O "Alterar senha" desativado da aba Segurança sai (para não ficar em dobro).

## Migração

- Coluna `VersaoSessao` (int, 0) em `Membros`.
- Tabela `CodigosRecuperacaoSenha`: `Id`, `MembroId` (FK), `CodigoHash`, `ExpiraEm`, `Tentativas`,
  `Usado`, `CriadoEm`.
- Nenhum dado existente muda (todos começam na versão 0; os tokens atuais, sem a versão, passam a ser
  recusados: todo mundo loga de novo uma vez).

## Etapas

1. Backend: `VersaoSessao` + conferência no JWT, `TrocarSenha` no domínio, `PUT api/Membro/senha`,
   limite de pedidos, migração.
2. Backend: código de recuperação, `IEnvioDeEmail` (MailKit + console no desenvolvimento), os dois
   endpoints de "esqueceu a senha".
3. Front: link no login, tela `/recuperar-senha`, bloco "Alterar senha" na aba Conta.

Cada etapa: build, `tsc`, `oxlint`, testes de ponta a ponta na API temporária (o código lido do log
no desenvolvimento), análise de padrão. **A migração só é aplicada no banco local com confirmação.**
Depois, o usuário configura o e-mail real no User Secrets pelo terminal dele.
