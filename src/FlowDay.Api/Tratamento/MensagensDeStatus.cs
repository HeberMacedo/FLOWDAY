using Microsoft.AspNetCore.Mvc;

namespace FlowDay.Api.Tratamento;

/// <summary>
/// Títulos e mensagens em português para as respostas de erro geradas pelo próprio ASP.NET Core,
/// como rota inexistente (404), método não permitido (405) ou corpo fora de JSON (415), que
/// chegariam em inglês e sem detalhe (RNF20).
/// </summary>
public static class MensagensDeStatus
{
    private static readonly Dictionary<int, (string Titulo, string Detalhe)> Mensagens = new()
    {
        [StatusCodes.Status400BadRequest] = ("Requisição inválida", "Não foi possível ler os dados enviados."),
        [StatusCodes.Status401Unauthorized] = ("Não autorizado", "Sessão ausente ou expirada. Faça login novamente."),
        [StatusCodes.Status403Forbidden] = ("Acesso negado", "Você não tem permissão para acessar este recurso."),
        [StatusCodes.Status404NotFound] = ("Não encontrado",
            "O endereço solicitado não existe ou o identificador informado está em formato inválido."),
        [StatusCodes.Status405MethodNotAllowed] = ("Método não permitido",
            "Esta rota não aceita o método HTTP utilizado."),
        [StatusCodes.Status406NotAcceptable] = ("Formato não aceito", "A API responde apenas em JSON."),
        [StatusCodes.Status413PayloadTooLarge] = ("Requisição muito grande",
            "Os dados enviados excedem o tamanho permitido."),
        [StatusCodes.Status415UnsupportedMediaType] = ("Formato não suportado",
            "Envie os dados em JSON (Content-Type: application/json)."),
        [StatusCodes.Status500InternalServerError] = ("Erro interno",
            "Ocorreu um erro inesperado. Tente novamente em instantes.")
    };

    /// <summary>
    /// Preenche título e detalhe quando a resposta veio do framework, sem mensagem própria.
    /// As respostas montadas pelo <see cref="TratadorDeExcecoes"/> já têm detalhe e não são alteradas.
    /// </summary>
    public static void Completar(ProblemDetails problema)
    {
        if (!string.IsNullOrWhiteSpace(problema.Detail) || problema.Status is not int status) return;
        if (!Mensagens.TryGetValue(status, out var mensagem)) return;

        problema.Title = mensagem.Titulo;
        problema.Detail = mensagem.Detalhe;
    }
}
