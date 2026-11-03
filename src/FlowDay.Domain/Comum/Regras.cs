using System.Text.RegularExpressions;

namespace FlowDay.Domain.Comum;

/// <summary>
/// Validações de formato reunidas em um só lugar. Cada método devolve a mensagem
/// de erro ou null quando o valor é válido, para que a camada de aplicação possa
/// juntar vários erros em uma única resposta.
/// </summary>
public static partial class Regras
{
    public const int NomeMin = 2, NomeMax = 100;
    public const int EmailMax = 254;
    public const int SenhaMin = 8, SenhaMax = 128;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    public static string? ValidarNome(string? nome)
    {
        var n = nome?.Trim() ?? string.Empty;
        return n.Length is < NomeMin or > NomeMax
            ? $"O nome deve ter de {NomeMin} a {NomeMax} caracteres."
            : null;
    }

    public static string? ValidarEmail(string? email)
    {
        var e = email?.Trim() ?? string.Empty;
        if (e.Length == 0) return "Informe o e-mail.";
        if (e.Length > EmailMax || !EmailRegex().IsMatch(e)) return "Informe um e-mail válido.";
        return null;
    }

    /// <summary>RN03: mínimo de 8 caracteres, com ao menos uma letra e um número.</summary>
    public static string? ValidarSenha(string? senha)
    {
        var s = senha ?? string.Empty;
        if (s.Length < SenhaMin || s.Length > SenhaMax)
            return $"A senha deve ter no mínimo {SenhaMin} caracteres.";
        if (!s.Any(char.IsLetter) || !s.Any(char.IsDigit))
            return "A senha deve ter ao menos uma letra e um número.";
        return null;
    }

    /// <summary>RN02: e-mail sem diferenciar maiúsculas de minúsculas.</summary>
    public static string NormalizarEmail(string email) => email.Trim().ToLowerInvariant();

    /// <summary>Remove a fração de segundo para que o valor seja igual depois de gravado no banco.</summary>
    public static DateTime TruncarSegundos(DateTime data) =>
        new(data.Ticks - data.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
}
