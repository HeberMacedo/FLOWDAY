using FlowDay.Application.Dtos;
using FlowDay.Application.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowDay.Api.Controllers;

/// <summary>Indicadores de produtividade (M6).</summary>
[ApiController]
[Authorize]
[Route("api/painel")]
[Produces("application/json")]
[Tags("Painel")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public class PainelController : ControllerBase
{
    private readonly PainelServico _servico;

    public PainelController(PainelServico servico) => _servico = servico;

    /// <summary>
    /// Totais de tarefas A fazer, Em andamento, concluídas na semana (segunda a domingo)
    /// e atrasadas, além das tarefas com prazo para hoje (RF42 e RF43).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PainelResponse), StatusCodes.Status200OK)]
    public Task<PainelResponse> Obter(CancellationToken ct) => _servico.ObterAsync(ct);
}
