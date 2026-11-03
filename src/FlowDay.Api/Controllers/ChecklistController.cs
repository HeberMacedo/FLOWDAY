using FlowDay.Application.Dtos;
using FlowDay.Application.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowDay.Api.Controllers;

/// <summary>Itens de checklist de uma tarefa (M3). Todas as rotas retornam a tarefa atualizada.</summary>
[ApiController]
[Authorize]
[Route("api/tarefas/{tarefaId:guid}/itens")]
[Produces("application/json")]
[Tags("Checklist")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
public class ChecklistController : ControllerBase
{
    private readonly ChecklistServico _servico;

    public ChecklistController(ChecklistServico servico) => _servico = servico;

    /// <summary>Adiciona um item (máximo de 50 por tarefa, RN19).</summary>
    /// <response code="422">Tarefa concluída ou limite de itens atingido.</response>
    [HttpPost]
    [ProducesResponseType(typeof(TarefaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TarefaResponse>> Adicionar(Guid tarefaId, ItemRequest req, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await _servico.AdicionarAsync(tarefaId, req, ct));

    /// <summary>Edita o texto de um item.</summary>
    [HttpPut("{itemId:guid}")]
    [ProducesResponseType(typeof(TarefaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public Task<TarefaResponse> Editar(Guid tarefaId, Guid itemId, ItemRequest req, CancellationToken ct) =>
        _servico.EditarAsync(tarefaId, itemId, req, ct);

    /// <summary>Marca ou desmarca um item. Não conclui a tarefa automaticamente (RN10).</summary>
    [HttpPatch("{itemId:guid}/concluido")]
    [ProducesResponseType(typeof(TarefaResponse), StatusCodes.Status200OK)]
    public Task<TarefaResponse> Marcar(Guid tarefaId, Guid itemId, MarcarItemRequest req, CancellationToken ct) =>
        _servico.MarcarAsync(tarefaId, itemId, req, ct);

    /// <summary>Exclui um item.</summary>
    [HttpDelete("{itemId:guid}")]
    [ProducesResponseType(typeof(TarefaResponse), StatusCodes.Status200OK)]
    public Task<TarefaResponse> Remover(Guid tarefaId, Guid itemId, CancellationToken ct) =>
        _servico.RemoverAsync(tarefaId, itemId, ct);
}
