using FlowDay.Application.Abstracoes;
using FlowDay.Application.Dtos;
using FlowDay.Application.Excecoes;
using FlowDay.Domain.Comum;
using FlowDay.Domain.Entidades;

namespace FlowDay.Application.Servicos;

/// <summary>UC06: CRUD de áreas (RN16, RN17, RN18).</summary>
public class AreaServico
{
    private readonly IAreaRepositorio _areas;
    private readonly ITarefaRepositorio _tarefas;
    private readonly IUnidadeDeTrabalho _uow;
    private readonly IUsuarioAtual _atual;
    private readonly IRelogio _relogio;

    public AreaServico(IAreaRepositorio areas, ITarefaRepositorio tarefas, IUnidadeDeTrabalho uow,
        IUsuarioAtual atual, IRelogio relogio)
    {
        _areas = areas;
        _tarefas = tarefas;
        _uow = uow;
        _atual = atual;
        _relogio = relogio;
    }

    public async Task<IReadOnlyList<AreaResponse>> ListarAsync(CancellationToken ct = default) =>
        (await _areas.ListarAsync(_atual.Id, ct))
            .OrderByDescending(a => a.EhGeral)
            .ThenBy(a => a.Nome)
            .Select(AreaResponse.De)
            .ToList();

    public async Task<AreaResponse> ObterAsync(Guid id, CancellationToken ct = default) =>
        AreaResponse.De(await ObterDoUsuarioAsync(id, ct));

    public async Task<AreaResponse> CriarAsync(AreaRequest req, CancellationToken ct = default)
    {
        await GarantirNomeDisponivelAsync(req.Nome, null, ct);
        var area = new Area(_atual.Id, req.Nome);
        _areas.Adicionar(area);
        await _uow.SalvarAsync(ct);
        return AreaResponse.De(area);
    }

    public async Task<AreaResponse> RenomearAsync(Guid id, AreaRequest req, CancellationToken ct = default)
    {
        var area = await ObterDoUsuarioAsync(id, ct);
        area.GarantirQuePodeSerRenomeada();
        await GarantirNomeDisponivelAsync(req.Nome, area.Id, ct);
        area.Renomear(req.Nome);
        await _uow.SalvarAsync(ct);
        return AreaResponse.De(area);
    }

    /// <summary>
    /// RN18: as tarefas da área vão para a Geral. As mudanças são gravadas em um único
    /// SaveChanges, que o EF Core executa dentro de uma transação (RNF21).
    /// </summary>
    public async Task ExcluirAsync(Guid id, CancellationToken ct = default)
    {
        var area = await ObterDoUsuarioAsync(id, ct);
        area.GarantirQuePodeSerExcluida();

        var geral = await _areas.ObterGeralAsync(_atual.Id, ct);
        var agora = _relogio.Agora;
        foreach (var tarefa in await _tarefas.ListarDaAreaAsync(area.Id, _atual.Id, ct))
            tarefa.MoverParaArea(geral.Id, agora);

        _areas.Remover(area);
        await _uow.SalvarAsync(ct);
    }

    private async Task GarantirNomeDisponivelAsync(string? nome, Guid? exceto, CancellationToken ct)
    {
        var erro = Area.ValidarNome(nome);
        if (erro is not null) throw new ValidacaoException("nome", erro);

        if (await _areas.NomeEmUsoAsync(_atual.Id, Area.Normalizar(nome!), exceto, ct))
            throw new ConflitoException("nome", "Já existe uma área com esse nome.");
    }

    private async Task<Area> ObterDoUsuarioAsync(Guid id, CancellationToken ct) =>
        await _areas.ObterAsync(id, _atual.Id, ct) ?? throw new NaoEncontradoException("Área não encontrada.");
}
