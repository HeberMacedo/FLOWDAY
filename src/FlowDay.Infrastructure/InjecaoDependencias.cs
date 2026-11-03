using FlowDay.Application.Abstracoes;
using FlowDay.Infrastructure.Persistencia;
using FlowDay.Infrastructure.Repositorios;
using FlowDay.Infrastructure.Servicos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FlowDay.Infrastructure;

public static class InjecaoDependencias
{
    /// <summary>Versão do MySQL usada no docker-compose.</summary>
    private static readonly MySqlServerVersion VersaoMySql = new(new Version(8, 4, 0));

    public static IServiceCollection AdicionarInfraestrutura(this IServiceCollection services, IConfiguration config)
    {
        var conexao = config.GetConnectionString("FlowDay")
            ?? throw new InvalidOperationException(
                "Connection string 'FlowDay' não configurada. Defina a variável ConnectionStrings__FlowDay.");

        services.AddDbContext<FlowDayDbContext>(o => o.UseMySql(conexao, VersaoMySql));
        services.AddScoped<IUnidadeDeTrabalho>(sp => sp.GetRequiredService<FlowDayDbContext>());

        services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
        services.AddScoped<IAreaRepositorio, AreaRepositorio>();
        services.AddScoped<ITarefaRepositorio, TarefaRepositorio>();
        services.AddScoped<IRecuperacaoSenhaRepositorio, RecuperacaoSenhaRepositorio>();

        services.Configure<OpcoesEmail>(config.GetSection("Email"));
        services.AddSingleton<IHashSenha, HashSenhaBCrypt>();
        services.AddSingleton<IRelogio, RelogioSistema>();
        services.AddScoped<IEnviadorEmail, EnviadorEmailSmtp>();

        return services;
    }

    /// <summary>
    /// Cria o banco na primeira execução. Tenta algumas vezes porque o container
    /// do MySQL pode levar alguns segundos para aceitar conexões.
    /// </summary>
    public static async Task InicializarBancoAsync(this IServiceProvider services, ILogger logger,
        int tentativas = 10)
    {
        for (var tentativa = 1; ; tentativa++)
        {
            try
            {
                using var escopo = services.CreateScope();
                var db = escopo.ServiceProvider.GetRequiredService<FlowDayDbContext>();
                await db.Database.EnsureCreatedAsync();
                logger.LogInformation("Banco de dados pronto.");
                return;
            }
            catch (Exception ex) when (tentativa < tentativas)
            {
                logger.LogWarning("Banco indisponível (tentativa {Tentativa}/{Total}): {Mensagem}",
                    tentativa, tentativas, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }
    }
}
