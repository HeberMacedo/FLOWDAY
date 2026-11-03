using FlowDay.Application.Dtos;
using FlowDay.Application.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FlowDay.Api.Controllers;

/// <summary>Cadastro, login, logout e recuperação de senha (M1).</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
[Tags("Autenticação")]
public class AutenticacaoController : ControllerBase
{
    private readonly AutenticacaoServico _servico;

    public AutenticacaoController(AutenticacaoServico servico) => _servico = servico;

    /// <summary>Cria a conta e já autentica o usuário.</summary>
    /// <remarks>Cria também as áreas Pessoal, Trabalho, Estudos, Lazer e Geral (RF01 a RF03).</remarks>
    /// <response code="201">Conta criada. Retorna o token de acesso.</response>
    /// <response code="400">Dados inválidos (nome, e-mail, senha ou confirmação).</response>
    /// <response code="409">E-mail já cadastrado.</response>
    /// <response code="429">Muitas tentativas em pouco tempo.</response>
    [HttpPost("cadastro")]
    [AllowAnonymous]
    [EnableRateLimiting(Configuracoes.LimiteRequisicoes.Politica)]
    [ProducesResponseType(typeof(AutenticacaoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AutenticacaoResponse>> Cadastrar(CadastroRequest req, CancellationToken ct)
    {
        var resposta = await _servico.CadastrarAsync(req, ct);
        return StatusCode(StatusCodes.Status201Created, resposta);
    }

    /// <summary>Autentica com e-mail e senha.</summary>
    /// <remarks>Após 5 falhas consecutivas, o login fica bloqueado por 15 minutos (RN04).</remarks>
    /// <response code="200">Login realizado. Retorna o token de acesso (validade de 60 minutos).</response>
    /// <response code="401">E-mail ou senha incorretos (mensagem genérica).</response>
    /// <response code="423">Login bloqueado temporariamente.</response>
    /// <response code="429">Muitas tentativas em pouco tempo.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(Configuracoes.LimiteRequisicoes.Politica)]
    [ProducesResponseType(typeof(AutenticacaoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status423Locked)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public Task<AutenticacaoResponse> Entrar(LoginRequest req, CancellationToken ct) =>
        _servico.EntrarAsync(req, ct);

    /// <summary>Encerra a sessão no aparelho.</summary>
    /// <remarks>
    /// A API é stateless (RNF22): o logout é concluído quando o front-end descarta o token.
    /// O endpoint existe para padronizar o fluxo e confirmar que o token ainda era válido.
    /// </remarks>
    /// <response code="204">Sessão encerrada.</response>
    /// <response code="401">Sessão ausente ou expirada.</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public IActionResult Sair() => NoContent();

    /// <summary>Solicita o link de redefinição de senha.</summary>
    /// <remarks>A resposta é sempre a mesma, exista ou não o e-mail (RN06).</remarks>
    /// <response code="202">Pedido recebido.</response>
    [HttpPost("recuperar-senha")]
    [AllowAnonymous]
    [EnableRateLimiting(Configuracoes.LimiteRequisicoes.Politica)]
    [ProducesResponseType(typeof(MensagemResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> SolicitarRecuperacao(RecuperarSenhaRequest req, CancellationToken ct) =>
        Accepted(await _servico.SolicitarRecuperacaoAsync(req, ct));

    /// <summary>Define uma nova senha usando o código recebido por e-mail.</summary>
    /// <remarks>Invalida o link e todas as sessões anteriores (RN05, RN06).</remarks>
    /// <response code="200">Senha redefinida.</response>
    /// <response code="400">Nova senha fora do padrão.</response>
    /// <response code="422">Link inválido, expirado, já usado ou substituído.</response>
    [HttpPost("redefinir-senha")]
    [AllowAnonymous]
    [EnableRateLimiting(Configuracoes.LimiteRequisicoes.Politica)]
    [ProducesResponseType(typeof(MensagemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public Task<MensagemResponse> RedefinirSenha(RedefinirSenhaRequest req, CancellationToken ct) =>
        _servico.RedefinirSenhaAsync(req, ct);
}
