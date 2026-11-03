using FlowDay.Application.Servicos;
using Microsoft.Extensions.DependencyInjection;

namespace FlowDay.Application;

public static class InjecaoDependencias
{
    public static IServiceCollection AdicionarAplicacao(this IServiceCollection services)
    {
        services.AddScoped<AutenticacaoServico>();
        services.AddScoped<PerfilServico>();
        services.AddScoped<AreaServico>();
        services.AddScoped<TarefaServico>();
        services.AddScoped<ChecklistServico>();
        services.AddScoped<PainelServico>();
        return services;
    }
}
