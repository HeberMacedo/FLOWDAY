using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.RateLimiting;
using FlowDay.Api.Seguranca;
using FlowDay.Application.Abstracoes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace FlowDay.Api.Configuracoes;

public static class LimiteRequisicoes
{
    public const string Politica = "autenticacao";

    /// <summary>RNF08: limita cadastro, login e recuperação de senha por endereço IP.</summary>
    public static IServiceCollection AdicionarLimiteDeRequisicoes(this IServiceCollection services, IConfiguration config)
    {
        var limite = config.GetValue("LimiteRequisicoes:PorMinuto", 10);

        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddPolicy(Politica, http => RateLimitPartition.GetFixedWindowLimiter(
                http.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limite,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
            o.OnRejected = async (contexto, ct) =>
            {
                await EscreverProblema(contexto.HttpContext, StatusCodes.Status429TooManyRequests,
                    "Muitas tentativas", "Muitas tentativas em pouco tempo. Aguarde um minuto e tente novamente.", ct);
            };
        });
        return services;
    }

    internal static Task EscreverProblema(HttpContext http, int status, string titulo, string detalhe,
        CancellationToken ct = default)
    {
        http.Response.StatusCode = status;
        http.Response.ContentType = "application/problem+json";
        var problema = new ProblemDetails
        {
            Status = status,
            Title = titulo,
            Detail = detalhe,
            Instance = http.Request.Path
        };
        problema.Extensions["traceId"] = http.TraceIdentifier;
        return http.Response.WriteAsync(JsonSerializer.Serialize(problema, OpcoesJson), ct);
    }

    // Mantém acentos legíveis na resposta (ex.: "Não autorizado" em vez de "N\u00E3o autorizado").
    private static readonly JsonSerializerOptions OpcoesJson = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}

public static class Autenticacao
{
    /// <summary>JWT com expiração de 60 minutos; tokens anteriores a uma troca de senha são recusados (RNF04).</summary>
    public static IServiceCollection AdicionarAutenticacaoJwt(this IServiceCollection services, IConfiguration config)
    {
        var secao = config.GetSection("Jwt");
        services.Configure<OpcoesJwt>(secao);
        var opcoes = secao.Get<OpcoesJwt>() ?? new OpcoesJwt();

        if (string.IsNullOrWhiteSpace(opcoes.Chave) || opcoes.Chave.Length < 32)
            throw new InvalidOperationException(
                "Defina a variável Jwt__Chave com pelo menos 32 caracteres (veja o arquivo .env.example).");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.MapInboundClaims = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = opcoes.Emissor,
                    ValidateAudience = true,
                    ValidAudience = opcoes.Publico,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = ServicoTokenJwt.CriarChave(opcoes.Chave),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = ClaimsFlowDay.Nome
                };

                o.Events = new JwtBearerEvents
                {
                    // RN05: a sessão deixa de valer quando a senha muda ou a conta é excluída.
                    OnTokenValidated = async contexto =>
                    {
                        var sub = contexto.Principal?.FindFirst(ClaimsFlowDay.Sub)?.Value;
                        var carimbo = contexto.Principal?.FindFirst(ClaimsFlowDay.CarimboSeguranca)?.Value;
                        if (!Guid.TryParse(sub, out var id) || carimbo is null)
                        {
                            contexto.Fail("Token inválido.");
                            return;
                        }

                        var usuarios = contexto.HttpContext.RequestServices.GetRequiredService<IUsuarioRepositorio>();
                        var usuario = await usuarios.ObterPorIdAsync(id, contexto.HttpContext.RequestAborted);
                        if (usuario is null || usuario.CarimboSeguranca.ToString() != carimbo)
                            contexto.Fail("Sessão encerrada.");
                    },
                    // Resposta padronizada para requisições sem token válido (CA40).
                    OnChallenge = async contexto =>
                    {
                        contexto.HandleResponse();
                        await LimiteRequisicoes.EscreverProblema(contexto.HttpContext, StatusCodes.Status401Unauthorized,
                            "Não autorizado", "Sessão ausente ou expirada. Faça login novamente.");
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }
}

public static class DocumentacaoSwagger
{
    /// <summary>Swagger/OpenAPI com descrições, exemplos e autenticação Bearer (RNF26).</summary>
    public static IServiceCollection AdicionarSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(o =>
        {
            o.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "FlowDay API",
                Version = "v1",
                Description = "API REST do FlowDay, plataforma de checklist e produtividade. " +
                              "Para testar as rotas protegidas, faça login, copie o token e clique em Authorize. " +
                              "Erros seguem o formato Problem Details (RFC 7807)."
            });

            var esquema = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Cole apenas o token retornado no login ou no cadastro.",
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            };
            o.AddSecurityDefinition("Bearer", esquema);
            o.AddSecurityRequirement(new OpenApiSecurityRequirement { [esquema] = Array.Empty<string>() });

            foreach (var assembly in new[] { Assembly.GetExecutingAssembly(), typeof(IRelogio).Assembly })
            {
                var xml = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");
                if (File.Exists(xml)) o.IncludeXmlComments(xml);
            }
        });
        return services;
    }
}
