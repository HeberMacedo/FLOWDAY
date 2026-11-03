using FlowDay.Application.Excecoes;
using FlowDay.Domain.Comum;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FlowDay.Api.Tratamento;

/// <summary>
/// Converte as exceções em respostas padronizadas no formato Problem Details (RFC 7807),
/// com o código HTTP adequado e sem expor detalhes internos (RNF20).
/// </summary>
public class TratadorDeExcecoes : IExceptionHandler
{
    private readonly ILogger<TratadorDeExcecoes> _logger;
    private readonly IProblemDetailsService _problemas;

    public TratadorDeExcecoes(ILogger<TratadorDeExcecoes> logger, IProblemDetailsService problemas)
    {
        _logger = logger;
        _problemas = problemas;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception excecao, CancellationToken ct)
    {
        ProblemDetails? problema = excecao switch
        {
            ValidacaoException e => Validacao(e),
            NaoEncontradoException e => Criar(StatusCodes.Status404NotFound, "Não encontrado", e.Message),
            RegraNegocioException e => Criar(StatusCodes.Status422UnprocessableEntity, "Regra de negócio violada", e.Message),
            ConflitoException e => Conflito(e),
            NaoAutorizadoException e => Criar(StatusCodes.Status401Unauthorized, "Não autorizado", e.Message),
            ContaBloqueadaException e => Bloqueio(e),
            BadHttpRequestException => Criar(StatusCodes.Status400BadRequest, "Requisição inválida",
                "Não foi possível ler os dados enviados."),
            OperationCanceledException => Criar(499, "Requisição cancelada", "A requisição foi cancelada."),
            _ => null
        };

        if (problema is null)
        {
            _logger.LogError(excecao, "Erro não tratado em {Metodo} {Caminho}", http.Request.Method, http.Request.Path);
            problema = Criar(StatusCodes.Status500InternalServerError, "Erro interno",
                "Ocorreu um erro inesperado. Tente novamente em instantes.");
        }

        http.Response.StatusCode = problema.Status ?? StatusCodes.Status500InternalServerError;
        return await _problemas.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            ProblemDetails = problema,
            Exception = excecao
        });
    }

    private static ProblemDetails Criar(int status, string titulo, string detalhe) =>
        new() { Status = status, Title = titulo, Detail = detalhe };

    private static ProblemDetails Validacao(ValidacaoException e)
    {
        var problema = new ValidationProblemDetails(e.Erros.ToDictionary(k => k.Key, v => v.Value))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Dados inválidos",
            Detail = e.Erros.Count == 1 ? e.Erros.First().Value.First() : e.Message
        };
        return problema;
    }

    private static ProblemDetails Conflito(ConflitoException e) =>
        new ValidationProblemDetails(new Dictionary<string, string[]> { [e.Campo] = new[] { e.Message } })
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflito",
            Detail = e.Message
        };

    private static ProblemDetails Bloqueio(ContaBloqueadaException e)
    {
        var problema = Criar(StatusCodes.Status423Locked, "Login bloqueado", e.Message);
        problema.Extensions["bloqueadoAte"] = e.BloqueadoAte;
        return problema;
    }
}
