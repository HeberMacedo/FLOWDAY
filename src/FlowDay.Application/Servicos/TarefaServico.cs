using FlowDay.Application.Abstracoes;
using FlowDay.Application.Dtos;
using FlowDay.Domain.Comum;
using FlowDay.Domain.Entidades;
using FlowDay.Domain.Enums;

namespace FlowDay.Application.Servicos;

/// <summary>UC04, UC07 e UC08: tarefas, visualizações e filtros.</summary>
public class TarefaServico
{
    public const int TamanhoPaginaPadrao = 20, TamanhoPaginaMaximo = 100;
    private const int DiasMaximosCalendario = 62;

    private readonly ITarefaRepositorio _tarefas;
    private readonly IAreaRepositorio _areas;
    private readonly IUnidadeDeTrabalho _uow;
    private readonly IUsuarioAtual _atual;
    private readonly IRelogio _relogio;

    public TarefaServico(ITarefaRepositorio tarefas, IAreaRepositorio areas, IUnidadeDeTrabalho uow,
        IUsuarioAtual atual, IRelogio relogio)
    {
        _tarefas = tarefas;
        _areas = areas;
        _uow = uow;
        _atual = atual;
        _relogio = relogio;
    }

    /// <summary>RF14 e RF32: lista paginada e ordenável, com filtros combinados (RN23).</summary>
    public async Task<PaginaResponse<TarefaResponse>> ListarAsync(FiltroTarefas filtro, OrdenacaoTarefas ordenacao,
        int pagina = 1, int tamanho = TamanhoPaginaPadrao, CancellationToken ct = default)
    {
        pagina = Math.Max(1, pagina);
        tamanho = Math.Clamp(tamanho, 1, TamanhoPaginaMaximo);

        var (itens, total) = await _tarefas.ListarAsync(_atual.Id, filtro, ordenacao, pagina, tamanho, ct);
        var areas = await MapaDeAreasAsync(ct);
        var hoje = _relogio.Hoje;

        return new PaginaResponse<TarefaResponse>(
            itens.Select(t => TarefaResponse.De(t, hoje, areas.GetValueOrDefault(t.AreaId))).ToList(),
            pagina, tamanho, total, (int)Math.Ceiling(total / (double)tamanho));
    }

    /// <summary>RF33: colunas do Kanban. O filtro de status limita as colunas exibidas (RN23).</summary>
    public async Task<KanbanResponse> KanbanAsync(FiltroTarefas filtro, CancellationToken ct = default)
    {
        var tarefas = await _tarefas.ListarPorStatusAsync(_atual.Id, filtro, ct);
        var areas = await MapaDeAreasAsync(ct);
        var hoje = _relogio.Hoje;

        var status = filtro.Status.HasValue
            ? new[] { filtro.Status.Value }
            : new[] { StatusTarefa.AFazer, StatusTarefa.EmAndamento, StatusTarefa.Concluida };

        var colunas = status.Select(s =>
        {
            var daColuna = tarefas.Where(t => t.Status == s)
                .Select(t => TarefaResponse.De(t, hoje, areas.GetValueOrDefault(t.AreaId)))
                .ToList();
            return new ColunaKanbanResponse(s, s.Rotulo(), daColuna.Count, daColuna);
        }).ToList();

        return new KanbanResponse(colunas);
    }

    /// <summary>RF35 e RN20: só tarefas com prazo; vale o período exibido, não o filtro de período.</summary>
    public async Task<CalendarioResponse> CalendarioAsync(DateOnly inicio, DateOnly fim, FiltroTarefas filtro,
        CancellationToken ct = default)
    {
        if (fim < inicio)
            throw new ValidacaoException("fim", "A data final deve ser igual ou posterior à data inicial.");
        if (fim.DayNumber - inicio.DayNumber > DiasMaximosCalendario)
            throw new ValidacaoException("fim", $"O período do calendário pode ter no máximo {DiasMaximosCalendario} dias.");

        var tarefas = await _tarefas.ListarPorPeriodoAsync(_atual.Id, filtro, inicio, fim, ct);
        var areas = await MapaDeAreasAsync(ct);
        var hoje = _relogio.Hoje;

        return new CalendarioResponse(inicio, fim,
            tarefas.Select(t => TarefaResponse.De(t, hoje, areas.GetValueOrDefault(t.AreaId))).ToList());
    }

    public async Task<TarefaResponse> ObterAsync(Guid id, CancellationToken ct = default)
    {
        var tarefa = await ObterDoUsuarioAsync(id, ct);
        return await MontarAsync(tarefa, ct);
    }

    /// <summary>RF13: campos omitidos assumem prioridade Média e área Geral (RN08).</summary>
    public async Task<TarefaResponse> CriarAsync(CriarTarefaRequest req, CancellationToken ct = default)
    {
        var area = await ResolverAreaAsync(req.AreaId, ct);
        var tarefa = new Tarefa(_atual.Id, area.Id, req.Titulo, req.Descricao,
            req.Prioridade ?? Prioridade.Media, req.Prazo, _relogio.Hoje, _relogio.Agora);

        foreach (var texto in req.Itens ?? Enumerable.Empty<string>())
            tarefa.AdicionarItem(texto, _relogio.Agora);

        _tarefas.Adicionar(tarefa);
        await _uow.SalvarAsync(ct);
        return TarefaResponse.De(tarefa, _relogio.Hoje, area);
    }

    /// <summary>RF16: edição; o prazo só é validado quando é alterado (RN13).</summary>
    public async Task<TarefaResponse> AtualizarAsync(Guid id, AtualizarTarefaRequest req, CancellationToken ct = default)
    {
        var tarefa = await ObterDoUsuarioAsync(id, ct);
        var area = req.AreaId.HasValue && req.AreaId.Value != tarefa.AreaId
            ? await ResolverAreaAsync(req.AreaId, ct)
            : await _areas.ObterAsync(tarefa.AreaId, _atual.Id, ct);

        tarefa.Atualizar(req.Titulo, req.Descricao, area?.Id ?? tarefa.AreaId,
            req.Prioridade ?? tarefa.Prioridade, req.Prazo, _relogio.Hoje, _relogio.Agora);

        await _uow.SalvarAsync(ct);
        return TarefaResponse.De(tarefa, _relogio.Hoje, area);
    }

    /// <summary>RF18 e RF26: mudança de status, respeitando o checklist (RN10, RN11).</summary>
    public async Task<TarefaResponse> AlterarStatusAsync(Guid id, AlterarStatusRequest req, CancellationToken ct = default)
    {
        var tarefa = await ObterDoUsuarioAsync(id, ct);
        tarefa.AlterarStatus(req.Status, _relogio.Agora);
        await _uow.SalvarAsync(ct);
        return await MontarAsync(tarefa, ct);
    }

    /// <summary>RF17: excluir a tarefa remove também seus itens (RN21).</summary>
    public async Task ExcluirAsync(Guid id, CancellationToken ct = default)
    {
        var tarefa = await ObterDoUsuarioAsync(id, ct);
        _tarefas.Remover(tarefa);
        await _uow.SalvarAsync(ct);
    }

    internal async Task<Tarefa> ObterDoUsuarioAsync(Guid id, CancellationToken ct) =>
        await _tarefas.ObterAsync(id, _atual.Id, ct) ?? throw new NaoEncontradoException("Tarefa não encontrada.");

    internal async Task<TarefaResponse> MontarAsync(Tarefa tarefa, CancellationToken ct)
    {
        var area = await _areas.ObterAsync(tarefa.AreaId, _atual.Id, ct);
        return TarefaResponse.De(tarefa, _relogio.Hoje, area);
    }

    /// <summary>RN01: a área informada precisa ser do próprio usuário.</summary>
    private async Task<Area> ResolverAreaAsync(Guid? areaId, CancellationToken ct)
    {
        if (areaId is null) return await _areas.ObterGeralAsync(_atual.Id, ct);
        return await _areas.ObterAsync(areaId.Value, _atual.Id, ct)
               ?? throw new ValidacaoException("areaId", "Área inválida.");
    }

    private async Task<Dictionary<Guid, Area>> MapaDeAreasAsync(CancellationToken ct) =>
        (await _areas.ListarAsync(_atual.Id, ct)).ToDictionary(a => a.Id);
}
