# Correção das pendências menores (#6, #9, #8, #21, #7)

**Situação:** proposta para revisão do usuário (12/10/2026). Nada implementado.

**Pedido (12/10/2026):** documentar como corrigir as pendências menores abertas.

## Ordem sugerida

| Ordem | # | Tamanho | Por quê nessa ordem |
|---|---|---|---|
| 1 | #7 `extrairErro` | Pequeno, só front | Arruma a base que o #6 vai usar (mensagens de erro das telas novas) |
| 2 | #8 `npm audit` | Pequeno, só `package.json` | Uma linha + teste do Excel |
| 3 | #9 Símbolos nos PDFs | Pequeno, backend | Fonte de reserva + checagem automática |
| 4 | #21 Licença do QuestPDF | Sem código | Decisão do usuário; só muda algo se o projeto virar comercial |
| 5 | #6 Configurações | Grande, 3 partes | Cada parte vira especificação própria antes de implementar |

Cada item segue o fluxo do projeto: plano → execução → análise de padrão → commit só quando o usuário
pedir. O #6 passa antes por uma especificação por parte.

---

## #7: `extrairErro` copiado em 8 páginas

### O que existe hoje

A função está copiada em `Cadastro`, `Categorias`, `Configuracoes`, `Contas`, `ContasConjuntas`,
`Dashboard`, `Metas` e `Relatorios`, em **3 variações**:

| Variação | Páginas | Diferença |
|---|---|---|
| Completa (`mensagem` → `errors` → `title` → padrão) | Categorias, Configuracoes, Contas, ContasConjuntas, Dashboard, Metas | Igual na prática (duas escritas diferentes da mesma lógica) |
| Sem `title` e sem parâmetro de mensagem padrão | Cadastro | Padrão fixo "Não foi possível criar a conta." |
| Só `mensagem` | Relatorios | Ignora erros de validação do ASP.NET (`errors`/`title`) |

Além disso, `Login.tsx` e `TopBar.tsx` leem `response.data.mensagem` direto, sem a função.

### Correção

1. Criar `frontend/src/utils/erros.ts` com uma função só, a variação completa:

```ts
/**
 * Mensagem de erro de uma chamada à API, na ordem:
 * 1. `mensagem` do ErroApiResponse (middleware do backend e limite de tentativas);
 * 2. primeiro erro de validação do ASP.NET (`errors`);
 * 3. `title` da validação;
 * 4. o texto padrão informado por quem chama.
 */
export function extrairErro(err: unknown, padrao: string): string
```

2. Nas 8 páginas: apagar a cópia e importar de `../utils/erros`. No `Cadastro`, passar
   `'Não foi possível criar a conta.'` como padrão.
3. `Login.tsx` e `TopBar.tsx` passam a usar a mesma função (o Login mantém o padrão
   "Nome de usuário ou senha inválidos.").

**Efeito visível:** só em Relatórios, que passa a mostrar também erros de validação (antes caía
no texto padrão). Nas outras telas nada muda.

### Como testar

- `npx tsc -b` e `npx oxlint src`; `grep -rn "function extrairErro" src` deve achar só `utils/erros.ts`.
- No navegador: senha errada no login (mensagem da API), cadastro com e-mail repetido (`mensagem`),
  cadastro com senhas diferentes (`errors` da validação), 6ª tentativa de login (429).

---

## #8: `uuid` vulnerável via ExcelJS

### O que existe hoje

- `npm audit`: 2 avisos moderados ([GHSA-w5hq-g745-h8pq](https://github.com/advisories/GHSA-w5hq-g745-h8pq)):
  `uuid` < 11.1.1 não confere o tamanho do buffer em `v3`/`v5`/`v6` quando um `buf` é passado.
- Vem de `exceljs@4.4.0` → `uuid@8.3.2`. O ExcelJS 4.4.0 é a **última versão publicada** (sem
  atualização desde 12/2024), então "aguardar versão nova" pode não ter fim.
- O `npm audit fix --force` instalaria `exceljs@3.4.0` (versão antiga): **não usar**.
- O ExcelJS só usa `v4` (`const {v4: uuidv4} = require('uuid')`), que não é afetado. O risco real é
  baixo; o objetivo é tirar o aviso sem depender do ExcelJS.

### Correção

Forçar o `uuid` corrigido só dentro do ExcelJS, com `overrides` no `frontend/package.json`:

```json
"overrides": {
  "exceljs": {
    "uuid": "^11.1.1"
  }
}
```

O `uuid` 11.1.1 ainda tem versão CommonJS (`require`) e mantém a função `v4`, então o ExcelJS
continua funcionando. **Não** usar `uuid` 12 ou mais novo (só ESM: o `require` do ExcelJS quebraria).

### Como testar

- `npm install` e `npm audit` → 0 vulnerabilidades; `npm ls uuid` → `uuid@11.x` sob o `exceljs`.
- `npx tsc -b`, `npx oxlint src` e `npm run build`.
- No navegador: Relatórios → exportar Excel; abrir o arquivo e conferir abas, formatação e totais.

**Se o Excel quebrar:** desfazer o `overrides` e registrar a aceitação do risco (só `v4` é usado).

---

## #9: Símbolos que a fonte dos PDFs não tem

### O que existe hoje

- Os PDFs (QuestPDF 2026.9.1) usam a fonte padrão **Lato**, que não tem `▲`, `▼` e `−` (sinal de
  menos tipográfico). Hoje o código evita esses caracteres (comentário em
  `QuestPdfGeradorRelatorio.cs`, nos cards).
- Um texto do usuário com esses símbolos (nome de categoria, descrição de transação) sai com um
  quadrado no lugar, sem erro nenhum.

### Correção

1. **Fonte de reserva embutida:** adicionar o arquivo `DejaVuSans.ttf` (licença livre, cobre setas,
   símbolos matemáticos e a maioria dos alfabetos) em `GerenciadorContaFamiliar/Fonts/` como
   `EmbeddedResource`, e registrar no `Program.cs`, junto da licença do QuestPDF:

```csharp
// Fonte de reserva dos PDFs: cobre símbolos que a Lato não tem (▲ ▼ −)
FontManager.RegisterFontFromEmbeddedResource("GerenciadorContaFamiliar.Fonts.DejaVuSans.ttf");
```

2. **Usar como reserva** no estilo padrão (`PdfComum.ConfigurarPagina`):

```csharp
pagina.DefaultTextStyle(t => t.FontFamily(Fonts.Lato, "DejaVu Sans").FontSize(9).FontColor(Texto));
```

   A Lato continua sendo a fonte do texto; a DejaVu só entra nos caracteres que faltam.
3. **Não depender das fontes do Windows:** `QuestPDF.Settings.UseSystemFonts = false`, para o PDF
   sair igual em qualquer servidor.
4. **Checagem no desenvolvimento:** `QuestPDF.Settings.ThrowOnMissingTextGlyphs = true` só no
   ambiente Development, para que um caractere sem fonte dê erro nos testes em vez de virar
   quadrado.
5. Opcional: devolver as setas `▲▼` nos cards do relatório (o comentário atual sai).

### Como testar

- Lançar uma transação com descrição `Teste ▲▼ − ✓ € ç ã`, exportar o PDF de transações e conferir
  os símbolos (com `ThrowOnMissingTextGlyphs` ligado, a exportação não pode dar erro).
- Exportar o PDF do relatório e conferir que o visual com a Lato não mudou.
- Conferir a licença do arquivo da fonte antes de embutir (DejaVu: licença livre, permite
  redistribuir embutida).

---

## #21: Licença do QuestPDF

### O que existe hoje

`Program.cs` usa `LicenseType.Community`: gratuita para pessoas físicas, organizações sem fins
lucrativos e empresas com receita bruta anual abaixo de US$ 1 milhão. Acima disso, a licença paga
(Professional ou Enterprise) é obrigatória.

### Correção (decisão, sem código agora)

| Situação do projeto | O que fazer |
|---|---|
| Uso familiar/pessoal ou empresa pequena | **Nada**: a Community vale. Registrar a decisão e fechar a pendência |
| Virar produto de empresa com receita ≥ US$ 1 mi | Comprar a licença e trocar `LicenseType.Community` pelo tipo comprado |
| Não querer depender de licença comercial | Trocar a geração dos PDFs: no backend por **PdfSharp/MigraDoc** (MIT), ou no front por uma biblioteca JS. Mexe em `QuestPdfGeradorRelatorio`, `QuestPdfGeradorTransacoes` e `PdfComum`; vale uma especificação própria |

Antes de decidir, conferir as condições atuais em `https://www.questpdf.com/license/` (podem mudar
entre versões).

**Sugestão:** fechar como "Community vale para o uso atual" e reabrir só se o projeto virar comercial.

---

## #6: Abas de Configurações só visuais

### O que existe hoje

Só a aba **Notificações** é real (preferências salvas). As outras abas mostram campos desativados
com a etiqueta "Em breve". No backend já existem `PUT api/Membro` (editar o próprio perfil) e
`DELETE api/Membro` (que hoje **só inativa** o membro); não existe como ler o perfil completo nem
trocar a senha.

| Aba | Item | O que falta |
|---|---|---|
| Conta | Nome, usuário, e-mail, telefone, nascimento | Ler o perfil (`GET`) e salvar pelo `PUT` existente |
| Conta | Alterar foto | Upload e armazenamento de arquivo (fora do escopo; ver decisões) |
| Segurança | Alterar senha | Endpoint novo (senha atual + nova + confirmação) |
| Segurança | Encerrar outras sessões | O JWT não tem como ser revogado hoje |
| Segurança | Duas etapas, aviso de novo acesso | Precisam de envio de e-mail |
| Notificações | Canais e-mail e navegador | E-mail precisa de SMTP; navegador precisa de permissão do navegador |
| Preferências | Tema | Já existe o botão sol/lua; falta "Igual ao sistema" |
| Preferências | Moeda, formato de data | Só existe pt-BR/R$: não há o que escolher |
| Preferências | Primeiro dia da semana, conta padrão, período padrão, ocultar valores | Guardar a preferência e usá-la nas telas |
| Privacidade | Exportar meus dados | Planilha com contas, transações, categorias e metas |
| Privacidade | Excluir conta | O `DELETE` existente só inativa: não sai das conjuntas, não para as séries, não encerra a sessão |

### Correção em 3 partes

Cada parte vira uma especificação própria (`docs/`) para revisão, como nas funcionalidades anteriores.

**Parte A: Conta e Segurança**

- `GET api/Membro` (perfil do membro logado: nome, usuário, e-mail, telefone, nascimento).
- Aba Conta: carrega o perfil, edita e salva pelo `PUT api/Membro`; atualiza o nome/e-mail da sessão
  (`localStorage` e contexto) depois de salvar.
- `PUT api/Membro/senha` com `senhaAtual`, `novaSenha`, `confirmacaoSenha` (mesmas regras do
  cadastro: 6 a 100 caracteres, confirmação igual). Senha atual errada → 400 com mensagem.
  Usa o limite de tentativas do login para não virar outro caminho de força bruta.
- **Encerrar outras sessões:** campo `VersaoSessao` no `Membro` (migração), gravado no JWT;
  o `JwtBearer` recusa token com versão antiga (`OnTokenValidated`). Trocar a senha também sobe a
  versão. O navegador atual recebe um cookie novo na hora.
- Fica "Em breve": foto, duas etapas e aviso de novo acesso (dependem de e-mail/arquivos).

**Parte B: Preferências**

- Tema com 3 opções (Claro, Escuro, Igual ao sistema), ligado ao `useTema` existente.
- Moeda e formato de data: **sair da tela** (não há escolha real; regra "nada fictício") ou virar
  texto informativo "Real (R$) · dd/mm/aaaa".
- Primeiro dia da semana, conta padrão para novas transações, período padrão dos Relatórios e
  ocultar valores ao abrir: salvos por membro no backend, no mesmo padrão das preferências de
  notificação (entidade + `GET`/`PUT`), e aplicados no Dashboard, no filtro "Esta semana" e em
  Relatórios.

**Parte C: Privacidade**

- **Exportar meus dados:** planilha Excel montada no front com o ExcelJS que já existe (abas
  Contas, Transações, Categorias, Metas), a partir dos endpoints atuais.
- **Excluir conta:** confirmação com a senha; no backend, além de inativar, sair de todas as contas
  conjuntas (regra existente de "sair da conjunta", que já para as séries), parar as séries das
  contas próprias e apagar o cookie de sessão; o front faz logout e volta ao login.
- **Canais de notificação:** e-mail continua "Em breve" (precisa de servidor SMTP e de mais um
  segredo no User Secrets). Navegador: usar a API de notificações do navegador só enquanto o app
  está aberto — ou deixar "Em breve" (ver decisões).

### Decisões do usuário antes das especificações

1. Itens que dependem de e-mail ou de arquivos (foto, duas etapas, aviso de novo acesso, canal
   e-mail): continuam "Em breve" ou **saem da tela** até existirem?
2. Moeda e formato de data: sair da tela ou virar texto informativo?
3. Notificações do navegador: fazer agora (só com o app aberto) ou deixar "Em breve"?
4. Excluir conta: manter como inativação (os dados ficam no banco) ou apagar os dados de fato?
   Apagar de fato mexe em transações de contas conjuntas que outros membros ainda usam.

**Respostas do usuário (14/10/2026):** 1) os itens que dependem de e-mail ou arquivos **saem da tela**;
2) moeda e formato de data viram **texto informativo** ("Real (R$) · dd/mm/aaaa"); 3) notificações
do navegador **agora**, só com o app aberto; 4) excluir conta = **desativar, sair das conjuntas, parar
as séries das contas próprias e encerrar as sessões** (os dados ficam no banco), confirmando com a senha.

### Como testar (em cada parte)

Build, `tsc`, `oxlint`, testes de ponta a ponta na API temporária (membros `tstnotif_*` apagados no
fim) e conferência visual de cada aba no navegador, nos temas claro e escuro.

## Execução (14/10/2026)

Pedido do usuário: executar este plano inteiro e, no fim, um agente separado testar. Ordem:
#7 → #8 → #9 → #21 (decisão registrada) → #6 Parte A (Conta e Segurança) → Parte B (Preferências)
→ Parte C (Privacidade + notificações do navegador). Cada parte com teste vermelho antes, build,
`tsc`, `oxlint` e ponta a ponta; migração só com confirmação; commit só quando pedido.

## Resultado (14/10/2026)

- **#7:** `frontend/src/utils/erros.ts` único (lê `mensagem`, erros de validação e `title`); as 9
  cópias saíram e `Login`/`TopBar` usam a mesma função.
- **#8:** `overrides` do `uuid` (11.1.1) para o ExcelJS: `npm audit` com 0 vulnerabilidades.
- **#9:** DejaVu Sans embutida como fonte de reserva (licença em `GerenciadorContaFamiliar/Fonts/`),
  `UseSystemFonts = false` e `ThrowOnMissingTextGlyphs` em Development: ▲▼−✓ saem nos PDFs.
- **#21:** sem código; continua nas pendências até a decisão do usuário.
- **#6 Parte A:** aba Conta lê e salva o perfil (`GET`/`PUT api/Membro`; corrigido o `PUT` que
  sempre dava 400 pela validação do `Id`); "Alterar foto", duas etapas e aviso de novo acesso
  saíram; "Encerrar outras sessões" funciona (`POST api/Membro/encerrar-sessoes`).
- **#6 Parte B:** tabela `PreferenciasUso` (migração `PreferenciasDeUso`) e `GET`/`PUT
  api/Membro/preferencias`; tema Claro/Escuro/Igual ao sistema (neste navegador, o mesmo estado do
  botão do topo; o logout não apaga mais a escolha); moeda e data como texto; primeiro dia da semana
  ("Esta semana"), conta padrão (nova transação), período padrão dos Relatórios e ocultar valores
  (cards do painel com •••• até tocar).
- **#6 Parte C:** "Exportar meus dados" gera `meus-dados-<data>.xlsx` (abas Contas, Transações,
  Categorias e Metas; nas conjuntas entram as transações de todos, com a coluna "Lançada por
  você"); "Excluir conta" pede a senha (`DELETE api/Membro` com `{ senha }`): inativa, sai das
  conjuntas (o criador é avisado), para as séries das conjuntas e das contas próprias, derruba as
  sessões e apaga o cookie. **Quem criou conjuntas ativas recebe 409** e precisa excluí-las antes
  (o domínio não deixa o criador sair). Canal e-mail saiu da aba Notificações; notificações do
  navegador (API `Notification`, permissão do navegador, escolha por navegador) aparecem para as
  notificações novas do sino quando o app está aberto mas não à vista, respeitando o horário de
  silêncio.
