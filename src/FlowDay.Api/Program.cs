using System.Text.Json.Serialization;
using FlowDay.Api.Configuracoes;
using FlowDay.Api.Seguranca;
using FlowDay.Api.Tratamento;
using FlowDay.Application;
using FlowDay.Application.Abstracoes;
using FlowDay.Infrastructure;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// Camadas (arquitetura limpa): Domínio <- Aplicação <- Infraestrutura <- API.
builder.Services.AdicionarAplicacao();
builder.Services.AdicionarInfraestrutura(config);
builder.Services.AddSingleton(config.GetSection("Aplicacao").Get<OpcoesAplicacao>() ?? new OpcoesAplicacao());

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioAtual, UsuarioAtual>();
builder.Services.AddSingleton<IServicoToken, ServicoTokenJwt>();
builder.Services.AdicionarAutenticacaoJwt(config);
builder.Services.AdicionarLimiteDeRequisicoes(config);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(o =>
    {
        // Erros de leitura do corpo ou dos parâmetros seguem o mesmo padrão das demais respostas (RNF20).
        o.InvalidModelStateResponseFactory = contexto =>
        {
            var problema = new ValidationProblemDetails(contexto.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Dados inválidos",
                Detail = "Verifique os campos informados.",
                Instance = contexto.HttpContext.Request.Path
            };
            problema.Extensions["traceId"] = contexto.HttpContext.TraceIdentifier;
            return new BadRequestObjectResult(problema) { ContentTypes = { "application/problem+json" } };
        };
    });

builder.Services.AddProblemDetails(o =>
    o.CustomizeProblemDetails = contexto =>
    {
        contexto.ProblemDetails.Instance ??= contexto.HttpContext.Request.Path;
        contexto.ProblemDetails.Extensions["traceId"] = contexto.HttpContext.TraceIdentifier;
    });
builder.Services.AddExceptionHandler<TratadorDeExcecoes>();

// RNF06: CORS restrito ao endereço do front-end.
var origens = config.GetSection("Cors:Origens").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(origens).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AdicionarSwagger();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    if (config.GetValue("Https:Redirecionar", false)) app.UseHttpsRedirection();
}

app.UseSwagger();
app.UseSwaggerUI(o =>
{
    o.SwaggerEndpoint("/swagger/v1/swagger.json", "FlowDay API v1");
    o.DocumentTitle = "FlowDay API";
});

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

await app.Services.InicializarBancoAsync(app.Logger);
await app.RunAsync();

// Permite referenciar a API em testes de integração (WebApplicationFactory).
public partial class Program { }
