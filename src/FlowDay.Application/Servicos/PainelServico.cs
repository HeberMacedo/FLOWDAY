using FlowDay.Application.Abstracoes;
using FlowDay.Application.Dtos;

namespace FlowDay.Application.Servicos;

/// <summary>UC09: indicadores do painel (RF42 e RF43).</summary>
public class PainelServico
{
    private readonly ITarefaRepositorio _tarefas;
    private readonly IAreaRepositorio _areas;
    private readonly IUsuarioAtual _atual;
    private readonly IRelogio _relogio;

    public PainelServico(ITarefaRepositorio tarefas, IAreaRepositorio areas, IUsuarioAtual atual, IRelogio relogio)
    {
        _tarefas = tarefas;
        _areas = areas;
        _atual = atual;
        _relogio = relogio;
    }

    public async Task<PainelResponse> ObterAsync(CancellationToken ct = default)
    {
        var hoje = _relogio.Hoje;
        var (inicio, fim) = SemanaAtual(hoje);

        var resumo = await _tarefas.ObterResumoAsync(_atual.Id, hoje,
            _relogio.InicioDoDiaEmUtc(inicio), _relogio.InicioDoDiaEmUtc(fim), ct);

        var areas = (await _areas.ListarAsync(_atual.Id, ct)).ToDictionary(a => a.Id);
        var deHoje = (await _tarefas.ListarComPrazoEmAsync(_atual.Id, hoje, ct))
            .Select(t => TarefaResponse.De(t, hoje, areas.GetValueOrDefault(t.AreaId)))
            .ToList();

        return new PainelResponse(resumo.AFazer, resumo.EmAndamento, resumo.ConcluidasNaSemana,
            resumo.Atrasadas, deHoje);
    }

    /// <summary>Semana de segunda a domingo. O fim é exclusivo (segunda seguinte).</summary>
    public static (DateOnly Inicio, DateOnly FimExclusivo) SemanaAtual(DateOnly hoje)
    {
        var diasDesdeSegunda = ((int)hoje.DayOfWeek + 6) % 7;
        var inicio = hoje.AddDays(-diasDesdeSegunda);
        return (inicio, inicio.AddDays(7));
    }
}
