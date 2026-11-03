using System.Security.Claims;
using System.Text;
using FlowDay.Application.Abstracoes;
using FlowDay.Application.Excecoes;
using FlowDay.Domain.Entidades;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace FlowDay.Api.Seguranca;

public class OpcoesJwt
{
    /// <summary>Chave de assinatura. Vem de variável de ambiente, nunca do código (RNF04, RNF09).</summary>
    public string Chave { get; set; } = string.Empty;
    public string Emissor { get; set; } = "FlowDay";
    public string Publico { get; set; } = "FlowDay.Web";
    public int ExpiracaoMinutos { get; set; } = 60;
}

public static class ClaimsFlowDay
{
    public const string Sub = "sub";
    public const string Nome = "name";
    public const string Email = "email";

    /// <summary>Carimbo da última troca de senha. Tokens antigos deixam de valer (RN05).</summary>
    public const string CarimboSeguranca = "sv";
}

public class ServicoTokenJwt : IServicoToken
{
    private readonly OpcoesJwt _opcoes;
    private readonly IRelogio _relogio;

    public ServicoTokenJwt(IOptions<OpcoesJwt> opcoes, IRelogio relogio)
    {
        _opcoes = opcoes.Value;
        _relogio = relogio;
    }

    public TokenGerado Gerar(Usuario usuario)
    {
        var agora = _relogio.Agora;
        var expira = agora.AddMinutes(_opcoes.ExpiracaoMinutos);

        var descritor = new SecurityTokenDescriptor
        {
            Issuer = _opcoes.Emissor,
            Audience = _opcoes.Publico,
            IssuedAt = agora,
            NotBefore = agora,
            Expires = expira,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimsFlowDay.Sub, usuario.Id.ToString()),
                new Claim(ClaimsFlowDay.Nome, usuario.Nome),
                new Claim(ClaimsFlowDay.Email, usuario.Email),
                new Claim(ClaimsFlowDay.CarimboSeguranca, usuario.CarimboSeguranca.ToString())
            }),
            SigningCredentials = new SigningCredentials(CriarChave(_opcoes.Chave), SecurityAlgorithms.HmacSha256)
        };

        return new TokenGerado(new JsonWebTokenHandler().CreateToken(descritor), expira);
    }

    public static SymmetricSecurityKey CriarChave(string chave) => new(Encoding.UTF8.GetBytes(chave));
}

/// <summary>Lê o usuário autenticado a partir do token da requisição.</summary>
public class UsuarioAtual : IUsuarioAtual
{
    private readonly IHttpContextAccessor _http;

    public UsuarioAtual(IHttpContextAccessor http) => _http = http;

    public Guid Id
    {
        get
        {
            var valor = _http.HttpContext?.User.FindFirstValue(ClaimsFlowDay.Sub);
            return Guid.TryParse(valor, out var id)
                ? id
                : throw new NaoAutorizadoException("Sessão inválida. Faça login novamente.");
        }
    }
}
