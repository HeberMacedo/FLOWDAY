using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace FlowDay.Api.Tratamento;

/// <summary>
/// Padroniza os erros de leitura da requisição (corpo, rota e query string) antes de
/// chegarem aos serviços: mensagens em português e sem detalhes internos, como nomes
/// de tipos ou trechos de exceções do serializador (RNF13, RNF20).
/// </summary>
public static class ValidacaoDeEntrada
{
    public const string MensagemFormatoInvalido = "Valor ausente ou em formato inválido.";

    /// <summary>Traduz as mensagens padrão do model binding do ASP.NET Core.</summary>
    public static void TraduzirMensagens(DefaultModelBindingMessageProvider m)
    {
        m.SetMissingBindRequiredValueAccessor(campo => $"O parâmetro '{campo}' é obrigatório.");
        m.SetMissingKeyOrValueAccessor(() => "Informe um valor.");
        m.SetMissingRequestBodyRequiredValueAccessor(() => "O corpo da requisição é obrigatório.");
        m.SetValueMustNotBeNullAccessor(_ => "O valor não pode ser nulo.");
        m.SetAttemptedValueIsInvalidAccessor((_, campo) => $"O valor informado para '{campo}' é inválido.");
        m.SetNonPropertyAttemptedValueIsInvalidAccessor(_ => "O valor informado é inválido.");
        m.SetUnknownValueIsInvalidAccessor(campo => $"O valor informado para '{campo}' é inválido.");
        m.SetNonPropertyUnknownValueIsInvalidAccessor(() => "O valor informado é inválido.");
        m.SetValueIsInvalidAccessor(_ => "O valor informado é inválido.");
        m.SetValueMustBeANumberAccessor(campo => $"O campo '{campo}' deve ser um número.");
        m.SetNonPropertyValueMustBeANumberAccessor(() => "O valor deve ser um número.");
    }

    /// <summary>Resposta 400 no formato Problem Details para dados que não puderam ser lidos.</summary>
    public static IActionResult CriarResposta(ActionContext contexto)
    {
        var erros = new Dictionary<string, string[]>();
        foreach (var (chave, entrada) in contexto.ModelState)
        {
            if (entrada.Errors.Count == 0) continue;

            // Erros do serializador JSON chegam com a chave no formato "$.campo".
            var vemDoJson = chave == "$" || chave.StartsWith("$.", StringComparison.Ordinal);
            var mensagens = entrada.Errors
                .Select(e => vemDoJson || string.IsNullOrWhiteSpace(e.ErrorMessage) ? MensagemFormatoInvalido : e.ErrorMessage)
                .Distinct()
                .ToArray();
            erros[NomeDoCampo(chave)] = mensagens;
        }

        var problema = new ValidationProblemDetails(erros)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Dados inválidos",
            Detail = erros.Count == 1 ? erros.First().Value.First() : "Verifique os campos informados.",
            Instance = contexto.HttpContext.Request.Path
        };
        problema.Extensions["traceId"] = contexto.HttpContext.TraceIdentifier;
        return new BadRequestObjectResult(problema) { ContentTypes = { "application/problem+json" } };
    }

    private static string NomeDoCampo(string chave)
    {
        var campo = chave.TrimStart('$', '.');
        if (string.IsNullOrEmpty(campo)) return "corpo";
        return char.ToLowerInvariant(campo[0]) + campo[1..];
    }
}
