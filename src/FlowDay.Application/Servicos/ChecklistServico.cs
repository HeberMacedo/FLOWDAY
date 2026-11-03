using FlowDay.Application.Abstracoes;
using FlowDay.Application.Dtos;

namespace FlowDay.Application.Servicos;

/// <summary>UC05: itens de checklist (RN10, RN12, RN19).</summary>
public class ChecklistServico
{
    private readonly ITarefaRepositorio _tarefas;
    private readonly TarefaServico _tarefaServico;
    private readonly IUnidadeDeTrabalho _uow;
    private readonly IRelogio _relogio;

    public ChecklistServico(ITarefaRepositorio tarefas, TarefaServico tarefaServico, IUnidadeDeTrabalho uow,
        IRelogio relogio)
    {
        _tarefas = tarefas;
        _tarefaServico = tarefaServico;
        _uow = uow;
        _relogio = relogio;
    }

    public async Task<TarefaResponse> AdicionarAsync(Guid tarefaId, ItemRequest req, CancellationToken ct = default)
    {
        var tarefa = await _tarefaServico.ObterDoUsuarioAsync(tarefaId, ct);
        var item = tarefa.AdicionarItem(req.Texto, _relogio.Agora);
        _tarefas.RegistrarNovoItem(item);
        await _uow.SalvarAsync(ct);
        return await _tarefaServico.MontarAsync(tarefa, ct);
    }

    public async Task<TarefaResponse> EditarAsync(Guid tarefaId, Guid itemId, ItemRequest req, CancellationToken ct = default)
    {
        var tarefa = await _tarefaServico.ObterDoUsuarioAsync(tarefaId, ct);
        tarefa.EditarItem(itemId, req.Texto, _relogio.Agora);
        await _uow.SalvarAsync(ct);
        return await _tarefaServico.MontarAsync(tarefa, ct);
    }

    public async Task<TarefaResponse> MarcarAsync(Guid tarefaId, Guid itemId, MarcarItemRequest req, CancellationToken ct = default)
    {
        var tarefa = await _tarefaServico.ObterDoUsuarioAsync(tarefaId, ct);
        tarefa.MarcarItem(itemId, req.Concluido, _relogio.Agora);
        await _uow.SalvarAsync(ct);
        return await _tarefaServico.MontarAsync(tarefa, ct);
    }

    public async Task<TarefaResponse> RemoverAsync(Guid tarefaId, Guid itemId, CancellationToken ct = default)
    {
        var tarefa = await _tarefaServico.ObterDoUsuarioAsync(tarefaId, ct);
        tarefa.RemoverItem(itemId, _relogio.Agora);
        await _uow.SalvarAsync(ct);
        return await _tarefaServico.MontarAsync(tarefa, ct);
    }
}
