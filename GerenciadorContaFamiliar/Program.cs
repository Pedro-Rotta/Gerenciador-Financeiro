// ---- Framework ----
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using GerenciadorContaFamiliar.Controllers;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Infrastructure.Data;
using GerenciadorContaFamiliar.Middleware;
using GerenciadorContaFamiliar.Infrastructure.Repositories;
using GerenciadorContaFamiliar.Infrastructure.Services;
using GerenciadorContaFamiliar.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

// ---- Use Cases: Interfaces + Implementações ----
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Auth;
using GerenciadorContaFamiliar.Application.UseCases.Auth;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Categorias;
using GerenciadorContaFamiliar.Application.UseCases.Categorias;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Contas;
using GerenciadorContaFamiliar.Application.UseCases.Contas;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.ContasConjuntas;
using GerenciadorContaFamiliar.Application.UseCases.ContasConjuntas;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Convites;
using GerenciadorContaFamiliar.Application.UseCases.Convites;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Metas;
using GerenciadorContaFamiliar.Application.UseCases.Metas;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Membros;
using GerenciadorContaFamiliar.Application.UseCases.Membros;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Notificacoes;
using GerenciadorContaFamiliar.Application.UseCases.Notificacoes;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Recorrencias;
using GerenciadorContaFamiliar.Application.UseCases.Recorrencias;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Relatorios;
using GerenciadorContaFamiliar.Application.UseCases.Relatorios;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.SubCategorias;
using GerenciadorContaFamiliar.Application.UseCases.SubCategorias;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Transacoes;
using GerenciadorContaFamiliar.Application.UseCases.Transacoes;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Verificacao;
using GerenciadorContaFamiliar.Application.UseCases.Verificacao;

var builder = WebApplication.CreateBuilder(args);

// ================================================================
// SWAGGER COM SUPORTE A JWT (para testar endpoints autenticados)
// ================================================================

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "GerenciadorContaFamiliar", Version = "v1" });

    // Botão "Authorize" no Swagger para informar o token JWT
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira seu token JWT. Exemplo: <b>seu_token_aqui</b>"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ================================================================
// AUTENTICAÇÃO JWT
// ================================================================

// Segredos ficam fora do git: User Secrets no desenvolvimento, variáveis de ambiente em produção.
// O User Secrets só é lido no ambiente Development: a mensagem diz em qual ambiente a API está.
string ComoConfigurar(string chave) =>
    $"{chave} não configurado (ambiente: {builder.Environment.EnvironmentName}). " +
    $"Em Development: dotnet user-secrets set \"{chave}\" \"<valor>\" (pasta GerenciadorContaFamiliar). " +
    $"Fora dele (o User Secrets não é lido): variável de ambiente {chave.Replace(":", "__")}.";

var secretKey = builder.Configuration["JwtSettings:SecretKey"];
if (string.IsNullOrWhiteSpace(secretKey))
    throw new InvalidOperationException(ComoConfigurar("JwtSettings:SecretKey"));
if (Encoding.UTF8.GetByteCount(secretKey) < 32)
    throw new InvalidOperationException("JwtSettings:SecretKey precisa ter pelo menos 32 bytes.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero  // Token expira exatamente no horário informado
    };

    // O front manda o token no cookie de sessão; o cabeçalho Bearer continua valendo
    // para o Swagger e scripts (usado quando não há cookie).
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = contexto =>
        {
            if (contexto.Request.Cookies.TryGetValue(CookieDeSessao.Nome, out var token))
                contexto.Token = token;
            return Task.CompletedTask;
        },
        // Troca de senha derruba as sessões: o token precisa ter a versão atual do membro
        OnTokenValidated = async contexto =>
        {
            var principal = contexto.Principal!;
            if (!int.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var membroId) ||
                !int.TryParse(principal.FindFirst(JwtTokenService.ClaimVersaoSessao)?.Value, out var versao))
            {
                contexto.Fail("Sessão sem versão.");
                return;
            }
            var membros = contexto.HttpContext.RequestServices.GetRequiredService<IMembroRepository>();
            var membro = await membros.ObterPorIdAsync(membroId, contexto.HttpContext.RequestAborted);
            if (membro is null || !membro.Ativo || membro.VersaoSessao != versao)
                contexto.Fail("Sessão encerrada.");
        }
    };
});

builder.Services.AddAuthorization();

// ================================================================
// CORS — Cross-Origin Resource Sharing
// Define QUAIS origens (frontends) podem consumir esta API.
// A política é nomeada para poder ser aplicada no pipeline abaixo.
// ================================================================

var corsOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirFrontend", policy =>
    {
        policy.WithOrigins(corsOrigins)          // origens permitidas (lidas do appsettings.json)
              .AllowAnyHeader()                  // qualquer header (ex: Authorization, Content-Type)
              .AllowAnyMethod()                  // qualquer método (GET, POST, PUT, DELETE, OPTIONS)
              .AllowCredentials();               // permite enviar cookies/credenciais junto
    });
});

// ================================================================
// ATRÁS DE PROXY (Render): ProxyReverso:Ativo=true
// O IP real de quem acessa (limite de login por pessoa) e o HTTPS original (cookie Secure) vêm dos
// cabeçalhos X-Forwarded-*. Só os últimos ProxyReverso:Saltos valem (padrão 1; a Render pode
// acrescentar proxies internos): um X-Forwarded-For inventado por quem acessa fica à esquerda e é
// ignorado. Para descobrir o número certo de saltos: ProxyReverso:Diagnostico=true e GET /api/saude/ip.
// Desligado no PC: ninguém falsifica o IP mandando o cabeçalho.
// ================================================================

var atrasDeProxy = builder.Configuration.GetValue<bool>("ProxyReverso:Ativo");
if (atrasDeProxy)
{
    var saltos = builder.Configuration.GetValue("ProxyReverso:Saltos", 1);
    if (saltos < 1)
        throw new InvalidOperationException("ProxyReverso:Saltos precisa ser 1 ou mais.");

    builder.Services.Configure<ForwardedHeadersOptions>(opcoes =>
    {
        opcoes.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        opcoes.ForwardLimit = saltos;
        // A Render não publica uma lista fixa de IPs do proxy
        opcoes.KnownNetworks.Clear();
        opcoes.KnownProxies.Clear();
    });
}

// ================================================================
// LIMITES DE REQUISIÇÕES (por IP)
// Atrás de proxy (Render), o IP vem do X-Forwarded-For (ProxyReverso:Ativo);
// pelo proxy do Vite, no PC, todos chegam como 127.0.0.1.
// Global: 300 por minuto por IP em tudo (API e telas), menos o health check. Uma tela abre umas
// dezenas de chamadas; a rajada de 16/10/2026 (centenas de cadastros por minuto) estourou a banda.
// Por endpoint: login, recuperação de senha e cadastro, abaixo.
// ================================================================

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(contexto =>
        contexto.Request.Path.StartsWithSegments("/api/saude")
            ? RateLimitPartition.GetNoLimiter("saude")
            : RateLimitPartition.GetFixedWindowLimiter(
                contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 300,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));

    // Cadastro anônimo: 5 contas por hora por IP (uma família cria as suas sem esbarrar nisso)
    options.AddPolicy(AuthController.PoliticaCadastro, contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0
            }));

    options.AddPolicy(AuthController.PoliticaLogin, contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // "Esqueceu a senha" e "alterar senha": 5 pedidos a cada 10 minutos por IP, contados
    // separadamente em cada endpoint (um não gasta o limite do outro)
    options.AddPolicy(AuthController.PoliticaRecuperacao, contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            $"{contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido"}|{contexto.Request.Path.Value?.ToLowerInvariant()}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0
            }));

    // Mesmo formato de erro do ExceptionHandlingMiddleware. O global roda antes da política do
    // endpoint, então um 429 no login pode ser do global; os dois pedem 1 minuto.
    options.OnRejected = async (contexto, ct) =>
    {
        var resposta = contexto.HttpContext.Response;
        resposta.ContentType = "application/json; charset=utf-8";
        var politica = contexto.HttpContext.GetEndpoint()?.Metadata
            .GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;
        await resposta.WriteAsJsonAsync(new ErroApiResponse(
            Status: StatusCodes.Status429TooManyRequests,
            Mensagem: politica switch
            {
                AuthController.PoliticaRecuperacao => "Muitos pedidos de senha. Aguarde 10 minutos e tente de novo.",
                AuthController.PoliticaLogin => "Muitas tentativas de login. Aguarde 1 minuto e tente de novo.",
                AuthController.PoliticaCadastro => "Muitos cadastros feitos daqui. Aguarde 1 hora e tente de novo.",
                _ => "Muitas requisições. Aguarde 1 minuto e tente de novo."
            },
            TraceId: contexto.HttpContext.TraceIdentifier,
            Detalhes: null), ct);
    };
});

// ================================================================
// BANCO DE DADOS
// ================================================================

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(ComoConfigurar("ConnectionStrings:DefaultConnection"));

builder.Services.AddDbContext<GerenciadorContaFamiliarDbContext>(options =>
    options.UseNpgsql(ConexaoBanco.ComTempoMinimo(connectionString)));

builder.Services.AddScoped<IUnitOfWork>(
    sp => sp.GetRequiredService<GerenciadorContaFamiliarDbContext>());

// ================================================================
// SERVIÇOS DE INFRAESTRUTURA (hash de senha, token)
// ================================================================

builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();

// E-mail ("Esqueceu a senha"): SMTP com Email:Host configurado; sem ele, só no desenvolvimento,
// o e-mail vai para o console da API
if (!string.IsNullOrWhiteSpace(builder.Configuration["Email:Host"]))
    builder.Services.AddScoped<IEnvioDeEmail, SmtpEnvioDeEmail>();
else if (builder.Environment.IsDevelopment())
    builder.Services.AddScoped<IEnvioDeEmail, ConsoleEnvioDeEmail>();
else
    throw new InvalidOperationException(ComoConfigurar("Email:Host"));

// Limite de 1 verificação por hora por membro (em memória, vale para todas as requisições)
builder.Services.AddSingleton<ILimiteVerificacao, LimiteVerificacaoEmMemoria>();

// Membro logado (lido do JWT) — usado pela auditoria automática do DbContext
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioAtual, UsuarioAtualHttp>();

// ================================================================
// REPOSITÓRIOS  (Infrastructure) — ordem alfabética por entidade
// ================================================================

builder.Services.AddScoped<ICategoriaRepository, EfCategoriaRepository>();
builder.Services.AddScoped<ICodigoRecuperacaoSenhaRepository, EfCodigoRecuperacaoSenhaRepository>();
builder.Services.AddScoped<IPreferenciaUsoRepository, EfPreferenciaUsoRepository>();
builder.Services.AddScoped<IContaConjuntaRepository, EfContaConjuntaRepository>();
builder.Services.AddScoped<IContaRepository, EfContaRepository>();
builder.Services.AddScoped<IConviteContaConjuntaRepository, EfConviteContaConjuntaRepository>();
builder.Services.AddScoped<ILogAuditoriaRepository, EfLogAuditoriaRepository>();
builder.Services.AddScoped<IMembroRepository, EfMembroRepository>();
builder.Services.AddScoped<IMetaRepository, EfMetaRepository>();
builder.Services.AddScoped<IMetaTransacaoRepository, EfMetaTransacaoRepository>();
builder.Services.AddScoped<INotificacaoRepository, EfNotificacaoRepository>();
builder.Services.AddScoped<IPreferenciaNotificacaoRepository, EfPreferenciaNotificacaoRepository>();
builder.Services.AddScoped<IRecorrenciaRepository, EfRecorrenciaRepository>();
builder.Services.AddScoped<ISubCategoriaRepository, EfSubCategoriaRepository>();
builder.Services.AddScoped<ITransacaoRepository, EfTransacaoRepository>();

// ================================================================
// USE CASES  — ordem alfabética por pasta
// ================================================================

// ---- Auth ----
builder.Services.AddScoped<ILoginUseCase, LoginUseCase>();
builder.Services.AddScoped<IEsqueciSenhaUseCase, EsqueciSenhaUseCase>();
builder.Services.AddScoped<IRedefinirSenhaUseCase, RedefinirSenhaUseCase>();

// ---- Categorias ----
builder.Services.AddScoped<ICriarCategoriaUseCase, CriarCategoriaUseCase>();
builder.Services.AddScoped<IEditarCategoriaUseCase, EditarCategoriaUseCase>();
builder.Services.AddScoped<IExcluirCategoriaUseCase, ExcluirCategoriaUseCase>();
builder.Services.AddScoped<IListarCategoriaUseCase, ListarCategoriaUseCase>();

// ---- Contas ----
builder.Services.AddScoped<ICriarContaUseCase, CriarContaUseCase>();
builder.Services.AddScoped<IEditarContaUseCase, EditarContaUseCase>();
builder.Services.AddScoped<IExcluirContaUseCase, ExcluirContaUseCase>();
builder.Services.AddScoped<IListarContaUseCase, ListarContaUseCase>();
builder.Services.AddScoped<IAlterarContaPrincipalUseCase, AlterarContaPrincipalUseCase>();
builder.Services.AddScoped<IDefinirSaldoInicialUseCase, DefinirSaldoInicialUseCase>();

// ---- ContasConjuntas ----
builder.Services.AddScoped<ICriarContaConjuntaUseCase, CriarContaConjuntaUseCase>();
builder.Services.AddScoped<IEditarContaConjuntaUseCase, EditarContaConjuntaUseCase>();
builder.Services.AddScoped<IExcluirContaConjuntaUseCase, ExcluirContaConjuntaUseCase>();
builder.Services.AddScoped<IListarContaConjuntaUseCase, ListarContaConjuntaUseCase>();
builder.Services.AddScoped<IRemoverParticipanteUseCase, RemoverParticipanteUseCase>();
builder.Services.AddScoped<ISairContaConjuntaUseCase, SairContaConjuntaUseCase>();

// ---- Convites (contas conjuntas) ----
builder.Services.AddScoped<ICriarConviteUseCase, CriarConviteUseCase>();
builder.Services.AddScoped<IListarConvitesRecebidosUseCase, ListarConvitesRecebidosUseCase>();
builder.Services.AddScoped<IListarConvitesDaContaUseCase, ListarConvitesDaContaUseCase>();
builder.Services.AddScoped<IAceitarConviteUseCase, AceitarConviteUseCase>();
builder.Services.AddScoped<IRecusarConviteUseCase, RecusarConviteUseCase>();
builder.Services.AddScoped<ICancelarConviteUseCase, CancelarConviteUseCase>();

// ---- Metas ----
builder.Services.AddScoped<ICriarMetaUseCase, CriarMetaUseCase>();
builder.Services.AddScoped<IEditarMetaUseCase, EditarMetaUseCase>();
builder.Services.AddScoped<IExcluirMetaUseCase, ExcluirMetaUseCase>();
builder.Services.AddScoped<IListarMetaUseCase, ListarMetaUseCase>();
builder.Services.AddScoped<IVincularTransacaoMetaUseCase, VincularTransacaoMetaUseCase>();
builder.Services.AddScoped<IDesvincularTransacaoMetaUseCase, DesvincularTransacaoMetaUseCase>();
builder.Services.AddScoped<IProgressoDaMeta, ProgressoDaMeta>();

// ---- Membros ----
builder.Services.AddScoped<ICriarMembroUseCase, CriarMembroUseCase>();
builder.Services.AddScoped<IEditarMembroUseCase, EditarMembroUseCase>();
builder.Services.AddScoped<IExcluirMembroUseCase, ExcluirMembroUseCase>();
builder.Services.AddScoped<IAlterarSenhaUseCase, AlterarSenhaUseCase>();
builder.Services.AddScoped<IObterPerfilUseCase, ObterPerfilUseCase>();
builder.Services.AddScoped<IEncerrarOutrasSessoesUseCase, EncerrarOutrasSessoesUseCase>();
builder.Services.AddScoped<IObterPreferenciasUsoUseCase, ObterPreferenciasUsoUseCase>();
builder.Services.AddScoped<ISalvarPreferenciasUsoUseCase, SalvarPreferenciasUsoUseCase>();

// ---- Notificações ----
builder.Services.AddScoped<ICriarNotificacaoUseCase, CriarNotificacaoUseCase>();
builder.Services.AddScoped<IEditarNotificacaoUseCase, EditarNotificacaoUseCase>();
builder.Services.AddScoped<IExcluirNotificacaoUseCase, ExcluirNotificacaoUseCase>();
builder.Services.AddScoped<IListarNotificacaoUseCase, ListarNotificacaoUseCase>();
builder.Services.AddScoped<IObterPreferenciasNotificacaoUseCase, ObterPreferenciasNotificacaoUseCase>();
builder.Services.AddScoped<ISalvarPreferenciasNotificacaoUseCase, SalvarPreferenciasNotificacaoUseCase>();
builder.Services.AddScoped<IAvisosDeLancamento, AvisosDeLancamento>();

// ---- Recorrências ----
builder.Services.AddScoped<IPararRecorrenciaUseCase, PararRecorrenciaUseCase>();
builder.Services.AddScoped<IGeradorDeOcorrencias, GeradorDeOcorrencias>();
builder.Services.AddScoped<IListarOcorrenciasPrevistasUseCase, ListarOcorrenciasPrevistasUseCase>();

// ---- Relatórios ----
builder.Services.AddScoped<IRelatorioGastosUseCase, RelatorioGastosUseCase>();
builder.Services.AddScoped<IRelatorioResumoUseCase, RelatorioResumoUseCase>();
builder.Services.AddScoped<IExportarRelatorioPdfUseCase, ExportarRelatorioPdfUseCase>();
builder.Services.AddSingleton<IGeradorPdfRelatorio, QuestPdfGeradorRelatorio>();

// PDFs (Relatórios e Transações) com QuestPDF: licença Community
// (gratuita para empresas com receita anual abaixo de US$ 1 milhão)
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
// No desenvolvimento, caractere sem fonte dá erro (nos testes) em vez de virar um quadrado no PDF
QuestPDF.Settings.ThrowOnMissingTextGlyphs = builder.Environment.IsDevelopment();
// Só as fontes embutidas (Lato do QuestPDF + DejaVu Sans de reserva): o PDF sai igual em
// qualquer servidor, sem depender das fontes instaladas
QuestPDF.Settings.UseSystemFonts = false;
QuestPDF.Drawing.FontManager.RegisterFontFromEmbeddedResource("GerenciadorContaFamiliar.Fonts.DejaVuSans.ttf");

// ---- SubCategorias ----
builder.Services.AddScoped<ICriarSubCategoriaUseCase, CriarSubCategoriaUseCase>();
builder.Services.AddScoped<IEditarSubCategoriaUseCase, EditarSubCategoriaUseCase>();
builder.Services.AddScoped<IExcluirSubCategoriaUseCase, ExcluirSubCategoriaUseCase>();
builder.Services.AddScoped<IDesvincularSubCategoriaUseCase, DesvincularSubCategoriaUseCase>();
builder.Services.AddScoped<IVincularSubCategoriaUseCase, VincularSubCategoriaUseCase>();
builder.Services.AddScoped<IListarSubCategoriaUseCase, ListarSubCategoriaUseCase>();

// ---- Transações ----
builder.Services.AddScoped<ICriarTransacaoUseCase, CriarTransacaoUseCase>();
builder.Services.AddScoped<IEditarTransacaoUseCase, EditarTransacaoUseCase>();
builder.Services.AddScoped<IExcluirTransacaoUseCase, ExcluirTransacaoUseCase>();
builder.Services.AddScoped<IListarTransacaoUseCase, ListarTransacaoUseCase>();
builder.Services.AddScoped<IExportarTransacoesPdfUseCase, ExportarTransacoesPdfUseCase>();
builder.Services.AddSingleton<IGeradorPdfTransacoes, QuestPdfGeradorTransacoes>();

// ---- Verificação ao abrir o app ----
builder.Services.AddScoped<IExecutarVerificacaoUseCase, ExecutarVerificacaoUseCase>();
// Passos: rodam na ordem de registro
builder.Services.AddScoped<IPassoVerificacao, GerarOcorrenciasRecorrentes>();
builder.Services.AddScoped<IPassoVerificacao, AvisarTransacoesAtrasadas>();
builder.Services.AddScoped<IPassoVerificacao, LembrarVencimentos>();
builder.Services.AddScoped<IPassoVerificacao, AvisarPrazoDasMetas>();

// ================================================================
// PIPELINE HTTP
// ================================================================

builder.Services.AddControllers();

// Compressão (Brotli, senão Gzip) das telas, do JS e do JSON: o bundle cai de ~0,5 MB para ~150 KB.
// Em HTTPS há o risco BREACH quando um segredo sai no corpo junto de texto do usuário; aqui a
// sessão vai no cookie (nunca no corpo), então vale ligar.
builder.Services.AddResponseCompression(opcoes => opcoes.EnableForHttps = true);

var app = builder.Build();

// Primeiro de tudo: os próximos middlewares (limite, autenticação, HTTPS) já veem o IP e o esquema reais
if (atrasDeProxy)
{
    // Diagnóstico (GET /api/saude/ip): guarda o X-Forwarded-For como chegou, antes de ser consumido
    if (app.Configuration.GetValue<bool>("ProxyReverso:Diagnostico"))
        app.Use((contexto, proximo) =>
        {
            contexto.Items[SaudeController.ChaveForwardedForBruto] = contexto.Request.Headers["X-Forwarded-For"].ToString();
            return proximo();
        });
    app.UseForwardedHeaders();
}

// Middleware global de exceções: converte erros não tratados em JSON padronizado.
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Limites antes das telas: o global também conta os arquivos estáticos (não só a API)
app.UseRateLimiter();

app.UseResponseCompression();

// Telas do React (wwwroot, preenchida pelo Dockerfile). No PC a pasta não existe e as telas vêm do Vite.
// index.html sem cache (cada publicação aparece ao recarregar); /assets/ tem o hash no nome, então
// pode ficar em cache por um ano.
var arquivosDasTelas = new StaticFileOptions
{
    OnPrepareResponse = contexto =>
        contexto.Context.Response.Headers.CacheControl =
            contexto.Context.Request.Path.StartsWithSegments("/assets")
                ? "public, max-age=31536000, immutable"
                : "no-cache"
};
app.UseDefaultFiles();
app.UseStaticFiles(arquivosDasTelas);

// CORS precisa estar entre o roteamento e a autenticação.
// Assim, a resposta do preflight (OPTIONS) sai antes de exigir token.
app.UseCors("PermitirFrontend");

// A ordem importa: Authentication ANTES de Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Rotas /api/... inexistentes: 404 sem HTML (sem esta linha, o fallback abaixo devolveria a tela).
// As rotas reais dos controllers são mais específicas e têm prioridade sobre esta.
app.Map("/api/{**resto}", () => Results.NotFound());

// Qualquer outro endereço (ex.: /configuracoes ao recarregar) devolve a tela; o React Router mostra a
// página certa. No PC, sem wwwroot, continua 404.
app.MapFallbackToFile("index.html", arquivosDasTelas);

// Servidor (Banco:AplicarMigracoes=true): cria e atualiza as tabelas antes de atender. Se falhar, a API
// não sobe e o log diz por quê (a Render mantém a versão anterior no ar). No PC fica desligado: as
// migrações continuam pelo dotnet ef.
if (app.Configuration.GetValue<bool>("Banco:AplicarMigracoes"))
{
    using var escopo = app.Services.CreateScope();
    await escopo.ServiceProvider.GetRequiredService<GerenciadorContaFamiliarDbContext>().Database.MigrateAsync();
}

app.Run();
