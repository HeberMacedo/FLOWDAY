using FlowDay.Application.Dtos;
using FlowDay.Application.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowDay.Api.Controllers;

/// <summary>CRUD de áreas (RF28 a RF31).</summary>
[ApiController]
[Authorize]
[Route("api/areas")]
[Produces("application/json")]
[Tags("Áreas")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public class AreasController : ControllerBase
{
    private readonly AreaServico _servico;

    public AreasController(AreaServico servico) => _servico = servico;

    /// <summary>Lista as áreas do usuário, com a Geral primeiro.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AreaResponse>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<AreaResponse>> Listar(CancellationToken ct) => _servico.ListarAsync(ct);

    /// <summary>Consulta uma área.</summary>
    /// <response code="404">Área inexistente ou de outro usuário.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AreaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<AreaResponse> Obter(Guid id, CancellationToken ct) => _servico.ObterAsync(id, ct);

    /// <summary>Cria uma área personalizada.</summary>
    /// <response code="201">Área criada.</response>
    /// <response code="400">Nome vazio ou com mais de 50 caracteres.</response>
    /// <response code="409">Já existe área com esse nome.</response>
    [HttpPost]
    [ProducesResponseType(typeof(AreaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AreaResponse>> Criar(AreaRequest req, CancellationToken ct)
    {
        var area = await _servico.CriarAsync(req, ct);
        return CreatedAtAction(nameof(Obter), new { id = area.Id }, area);
    }

    /// <summary>Renomeia uma área. A Geral não pode ser renomeada (RN17).</summary>
    /// <response code="422">Tentativa de renomear a área Geral.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AreaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public Task<AreaResponse> Renomear(Guid id, AreaRequest req, CancellationToken ct) =>
        _servico.RenomearAsync(id, req, ct);

    /// <summary>Exclui a área e move suas tarefas para a Geral (RN18).</summary>
    /// <response code="204">Área excluída.</response>
    /// <response code="422">Tentativa de excluir a área Geral.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct)
    {
        await _servico.ExcluirAsync(id, ct);
        return NoContent();
    }
}
