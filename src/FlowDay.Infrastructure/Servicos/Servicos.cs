using System.Net;
using System.Net.Mail;
using FlowDay.Application.Abstracoes;
using Microsoft.Extensions.Options;

namespace FlowDay.Infrastructure.Servicos;

/// <summary>Hash de senha com BCrypt, que já inclui o salt (RNF03).</summary>
public class HashSenhaBCrypt : IHashSenha
{
    private const int Custo = 11;

    public string Gerar(string senha) => BCrypt.Net.BCrypt.HashPassword(senha, Custo);

    public bool Verificar(string senha, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(senha, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}

/// <summary>Relógio do sistema no fuso America/Sao_Paulo (P04).</summary>
public class RelogioSistema : IRelogio
{
    private static readonly TimeZoneInfo Fuso = CarregarFuso();

    public DateTime Agora => DateTime.UtcNow;

    public DateOnly Hoje => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Fuso));

    public DateTime InicioDoDiaEmUtc(DateOnly data) =>
        TimeZoneInfo.ConvertTimeToUtc(data.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), Fuso);

    private static TimeZoneInfo CarregarFuso()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch (Exception)
        {
            // Sem a base de fusos no sistema, usa o horário de Brasília fixo (UTC-3).
            return TimeZoneInfo.CreateCustomTimeZone("America/Sao_Paulo", TimeSpan.FromHours(-3),
                "Horário de Brasília", "Horário de Brasília");
        }
    }
}

public class OpcoesEmail
{
    public string Host { get; set; } = "localhost";
    public int Porta { get; set; } = 1025;
    public bool UsarSsl { get; set; }
    public string? Usuario { get; set; }
    public string? Senha { get; set; }
    public string Remetente { get; set; } = "nao-responda@flowday.local";
}

/// <summary>Envio por SMTP. Em desenvolvimento, aponta para o Mailpit do docker-compose.</summary>
public class EnviadorEmailSmtp : IEnviadorEmail
{
    private readonly OpcoesEmail _opcoes;

    public EnviadorEmailSmtp(IOptions<OpcoesEmail> opcoes) => _opcoes = opcoes.Value;

    public async Task EnviarAsync(string para, string assunto, string corpoHtml, CancellationToken ct = default)
    {
        using var mensagem = new MailMessage(_opcoes.Remetente, para, assunto, corpoHtml) { IsBodyHtml = true };
        using var cliente = new SmtpClient(_opcoes.Host, _opcoes.Porta) { EnableSsl = _opcoes.UsarSsl };

        if (!string.IsNullOrEmpty(_opcoes.Usuario))
            cliente.Credentials = new NetworkCredential(_opcoes.Usuario, _opcoes.Senha);

        await cliente.SendMailAsync(mensagem, ct);
    }
}
