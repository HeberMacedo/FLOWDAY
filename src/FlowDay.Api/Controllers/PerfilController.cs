using FlowDay.Application.Dtos;
using FlowDay.Application.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowDay.Api.Controllers;

/// <summary>Dados do usuário autenticado (RF09 a RF11).</summary>
[ApiController]
[Authorize]
[Route("api/perfil")]
[Produces("application/json")]
[Tags("Perfil")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public class PerfilController : ControllerBase
{
    private readonly PerfilServico _servico;

    public PerfilController(PerfilServico servico) => _servico = servico;

    /// <summary>Retorna os dados do usuário autenticado.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status200OK)]
    public Task<UsuarioResponse> Obter(CancellationToken ct) => _servico.ObterAsync(ct);

    /// <summary>Altera nome e e-mail. Alterar o e-mail exige a senha atual (RN22).</summary>
    /// <response code="200">Perfil atualizado.</response>
    /// <response code="400">Dados inválidos ou senha atual incorreta.</response>
    /// <response code="409">Novo e-mail já está em uso.</response>
    [HttpPut]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status409Conflict)]
    public Task<UsuarioResponse> Atualizar(AtualizarPerfilRequest req, CancellationToken ct) =>
        _servico.AtualizarAsync(req, ct);

    /// <summary>Altera a senha e invalida as sessões anteriores (RN05).</summary>
    /// <remarks>Retorna um novo token para manter o aparelho atual conectado.</remarks>
    /// <response code="200">Senha alterada.</response>
    /// <response code="400">Senha atual incorreta ou nova senha fora do padrão.</response>
    [HttpPut("senha")]
    [ProducesResponseType(typeof(AutenticacaoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public Task<AutenticacaoResponse> AlterarSenha(AlterarSenhaRequest req, CancellationToken ct) =>
        _servico.AlterarSenhaAsync(req, ct);

    /// <summary>Exclui a conta e todos os dados do usuário (RN21).</summary>
    /// <response code="204">Conta excluída.</response>
    /// <response code="400">Senha atual incorreta.</response>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Excluir([FromBody] ExcluirContaRequest req, CancellationToken ct)
    {
        await _servico.ExcluirContaAsync(req, ct);
        return NoContent();
    }
}
