using FlowDay.Application.Dtos;
using FlowDay.Application.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FlowDay.Api.Controllers;

/// <summary>CRUD de tarefas e visualizações em lista, Kanban e calendário (M2 e M5).</summary>
[ApiController]
[Authorize]
[Route("api/tarefas")]
[Produces("application/json")]
[Tags("Tarefas")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public class TarefasController : ControllerBase
{
    private readonly TarefaServico _servico;

    public TarefasController(TarefaServico servico) => _servico = servico;

    /// <summary>Lista as tarefas com paginação, ordenação, busca e filtros combinados.</summary>
    /// <param name="filtro">Busca por texto e filtros por área, prioridade, status e período de prazo (RN23).</param>
    /// <param name="ordenarPor">Prazo, Prioridade ou Criacao (padrão).</param>
    /// <param name="ordem">asc ou desc. Padrão: asc para prazo e desc para os demais.</param>
    /// <param name="pagina">Página, começando em 1.</param>
    /// <param name="tamanho">Itens por página (padrão 20, máximo 100).</param>
    /// <param name="ct"></param>
    [HttpGet]
    [ProducesResponseType(typeof(PaginaResponse<TarefaResponse>), StatusCodes.Status200OK)]
    public Task<PaginaResponse<TarefaResponse>> Listar([FromQuery] FiltroTarefas filtro,
        [FromQuery] CampoOrdenacao ordenarPor = CampoOrdenacao.Criacao, [FromQuery] string? ordem = null,
        [FromQuery] int pagina = 1, [FromQuery] int tamanho = TarefaServico.TamanhoPaginaPadrao,
        CancellationToken ct = default)
    {
        var decrescente = ordem?.Trim().ToLowerInvariant() switch
        {
            "asc" => false,
            "desc" => true,
            _ => ordenarPor != CampoOrdenacao.Prazo
        };

        var ordenacao = new OrdenacaoTarefas { OrdenarPor = ordenarPor, Decrescente = decrescente };
        return _servico.ListarAsync(filtro, ordenacao, pagina, tamanho, ct);
    }

    /// <summary>Quadro Kanban com as colunas A fazer, Em andamento e Concluída.</summary>
    /// <remarks>Com o filtro de status, apenas a coluna correspondente é retornada (RN23).</remarks>
    [HttpGet("kanban")]
    [ProducesResponseType(typeof(KanbanResponse), StatusCodes.Status200OK)]
    public Task<KanbanResponse> Kanban([FromQuery] FiltroTarefas filtro, CancellationToken ct) =>
        _servico.KanbanAsync(filtro, ct);

    /// <summary>Tarefas com prazo dentro do período exibido no calendário (RN20).</summary>
    /// <param name="inicio">Primeiro dia exibido (AAAA-MM-DD).</param>
    /// <param name="fim">Último dia exibido (AAAA-MM-DD). Período máximo de 62 dias.</param>
    /// <param name="filtro">Os filtros de período de prazo são ignorados aqui.</param>
    /// <param name="ct"></param>
    [HttpGet("calendario")]
    [ProducesResponseType(typeof(CalendarioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public Task<CalendarioResponse> Calendario([FromQuery, BindRequired] DateOnly inicio, [FromQuery, BindRequired] DateOnly fim,
        [FromQuery] FiltroTarefas filtro, CancellationToken ct) =>
        _servico.CalendarioAsync(inicio, fim, filtro, ct);

    /// <summary>Consulta uma tarefa com o checklist.</summary>
    /// <response code="404">Tarefa inexistente ou de outro usuário (RN01).</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TarefaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<TarefaResponse> Obter(Guid id, CancellationToken ct) => _servico.ObterAsync(id, ct);

    /// <summary>Cria uma tarefa. Sem área e prioridade, usa Geral e Média (RN08).</summary>
    /// <response code="201">Tarefa criada com status A fazer.</response>
    /// <response code="400">Título, descrição, prazo, área ou itens inválidos.</response>
    [HttpPost]
    [ProducesResponseType(typeof(TarefaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TarefaResponse>> Criar(CriarTarefaRequest req, CancellationToken ct)
    {
        var tarefa = await _servico.CriarAsync(req, ct);
        return CreatedAtAction(nameof(Obter), new { id = tarefa.Id }, tarefa);
    }

    /// <summary>Altera título, descrição, área, prioridade e prazo.</summary>
    /// <remarks>O prazo só é validado quando muda: editar outros campos de tarefa vencida é permitido (RN13).</remarks>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TarefaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<TarefaResponse> Atualizar(Guid id, AtualizarTarefaRequest req, CancellationToken ct) =>
        _servico.AtualizarAsync(id, req, ct);

    /// <summary>Altera o status (A fazer, Em andamento ou Concluída).</summary>
    /// <remarks>Concluir exige todos os itens do checklist concluídos (RN10). Reabrir remove a data de conclusão (RN11).</remarks>
    /// <response code="422">Há itens pendentes no checklist.</response>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(TarefaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public Task<TarefaResponse> AlterarStatus(Guid id, AlterarStatusRequest req, CancellationToken ct) =>
        _servico.AlterarStatusAsync(id, req, ct);

    /// <summary>Exclui a tarefa e seus itens (RN21).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct)
    {
        await _servico.ExcluirAsync(id, ct);
        return NoContent();
    }
}
