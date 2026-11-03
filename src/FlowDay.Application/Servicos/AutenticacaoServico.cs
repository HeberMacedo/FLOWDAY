using System.Security.Cryptography;
using System.Text;
using FlowDay.Application.Abstracoes;
using FlowDay.Application.Dtos;
using FlowDay.Application.Excecoes;
using FlowDay.Domain.Comum;
using FlowDay.Domain.Entidades;
using Microsoft.Extensions.Logging;

namespace FlowDay.Application.Servicos;

public class AutenticacaoServico
{
    public const string MensagemLoginInvalido = "E-mail ou senha incorretos.";
    public const string MensagemRecuperacao =
        "Se o e-mail estiver cadastrado, você receberá um link para redefinir a senha.";

    private readonly IUsuarioRepositorio _usuarios;
    private readonly IAreaRepositorio _areas;
    private readonly IRecuperacaoSenhaRepositorio _recuperacoes;
    private readonly IUnidadeDeTrabalho _uow;
    private readonly IHashSenha _hash;
    private readonly IServicoToken _tokens;
    private readonly IEnviadorEmail _email;
    private readonly IRelogio _relogio;
    private readonly OpcoesAplicacao _opcoes;
    private readonly ILogger<AutenticacaoServico> _logger;

    public AutenticacaoServico(IUsuarioRepositorio usuarios, IAreaRepositorio areas,
        IRecuperacaoSenhaRepositorio recuperacoes, IUnidadeDeTrabalho uow, IHashSenha hash,
        IServicoToken tokens, IEnviadorEmail email, IRelogio relogio, OpcoesAplicacao opcoes,
        ILogger<AutenticacaoServico> logger)
    {
        _usuarios = usuarios;
        _areas = areas;
        _recuperacoes = recuperacoes;
        _uow = uow;
        _hash = hash;
        _tokens = tokens;
        _email = email;
        _relogio = relogio;
        _opcoes = opcoes;
        _logger = logger;
    }

    /// <summary>UC01: cria a conta, as áreas padrão e já autentica o usuário (RN05, RN16).</summary>
    public async Task<AutenticacaoResponse> CadastrarAsync(CadastroRequest req, CancellationToken ct = default)
    {
        var erros = new Validador()
            .Campo("nome", Regras.ValidarNome(req.Nome))
            .Campo("email", Regras.ValidarEmail(req.Email))
            .Campo("senha", Regras.ValidarSenha(req.Senha))
            .Campo("confirmacaoSenha", req.Senha != req.ConfirmacaoSenha ? "A confirmação de senha não confere." : null);
        erros.LancarSeHouverErros();

        var emailNormalizado = Regras.NormalizarEmail(req.Email);
        if (await _usuarios.EmailEmUsoAsync(emailNormalizado, ct: ct))
            throw new ConflitoException("email", "Este e-mail já está em uso.");

        var usuario = new Usuario(req.Nome, req.Email, _hash.Gerar(req.Senha), _relogio.Agora);
        _usuarios.Adicionar(usuario);
        _areas.AdicionarVarias(Area.CriarPadrao(usuario.Id));
        await _uow.SalvarAsync(ct);

        return Autenticar(usuario);
    }

    /// <summary>UC02: login com mensagem genérica e bloqueio após 5 falhas (RN04).</summary>
    public async Task<AutenticacaoResponse> EntrarAsync(LoginRequest req, CancellationToken ct = default)
    {
        var agora = _relogio.Agora;
        var usuario = string.IsNullOrWhiteSpace(req.Email)
            ? null
            : await _usuarios.ObterPorEmailAsync(Regras.NormalizarEmail(req.Email), ct);

        if (usuario is null)
        {
            // Mesmo custo de uma verificação real, para não revelar se o e-mail existe.
            _ = _hash.Gerar(req.Senha ?? string.Empty);
            throw new NaoAutorizadoException(MensagemLoginInvalido);
        }

        if (usuario.EstaBloqueado(agora))
            throw new ContaBloqueadaException(usuario.BloqueadoAte!.Value);

        if (!_hash.Verificar(req.Senha ?? string.Empty, usuario.SenhaHash))
        {
            usuario.RegistrarFalhaLogin(agora);
            await _uow.SalvarAsync(ct);
            throw new NaoAutorizadoException(MensagemLoginInvalido);
        }

        usuario.RegistrarLoginComSucesso();
        await _uow.SalvarAsync(ct);
        return Autenticar(usuario);
    }

    /// <summary>UC03: a resposta é a mesma exista ou não o e-mail (RN06).</summary>
    public async Task<MensagemResponse> SolicitarRecuperacaoAsync(RecuperarSenhaRequest req, CancellationToken ct = default)
    {
        var resposta = new MensagemResponse(MensagemRecuperacao);
        if (Regras.ValidarEmail(req.Email) is not null) return resposta;

        var usuario = await _usuarios.ObterPorEmailAsync(Regras.NormalizarEmail(req.Email), ct);
        if (usuario is null) return resposta;

        var agora = _relogio.Agora;
        foreach (var anterior in await _recuperacoes.ListarAtivasAsync(usuario.Id, agora, ct))
            anterior.Encerrar(agora); // nova solicitação invalida o link anterior

        var codigo = GerarCodigo();
        _recuperacoes.Adicionar(new RecuperacaoSenha(usuario.Id, CalcularHash(codigo), agora));
        await _uow.SalvarAsync(ct);

        var link = $"{_opcoes.UrlFrontend.TrimEnd('/')}/redefinir-senha.html?token={Uri.EscapeDataString(codigo)}";
        try
        {
            await _email.EnviarAsync(usuario.Email, "FlowDay - Redefinição de senha",
                $"<p>Olá, {System.Net.WebUtility.HtmlEncode(usuario.Nome)}.</p>" +
                "<p>Recebemos um pedido para redefinir a sua senha no FlowDay.</p>" +
                $"<p><a href=\"{link}\">Clique aqui para criar uma nova senha</a>. O link vale por 30 minutos e só pode ser usado uma vez.</p>" +
                "<p>Se você não fez esse pedido, ignore este e-mail.</p>", ct);
        }
        catch (Exception ex)
        {
            // Não registra o código nem o link (RNF11).
            _logger.LogWarning(ex, "Falha ao enviar o e-mail de recuperação de senha.");
        }

        return resposta;
    }

    /// <summary>UC03: define a nova senha, invalida o link e as sessões anteriores (RN04, RN05, RN06).</summary>
    public async Task<MensagemResponse> RedefinirSenhaAsync(RedefinirSenhaRequest req, CancellationToken ct = default)
    {
        new Validador()
            .Campo("novaSenha", Regras.ValidarSenha(req.NovaSenha))
            .Campo("confirmacaoSenha", req.NovaSenha != req.ConfirmacaoSenha ? "A confirmação de senha não confere." : null)
            .LancarSeHouverErros();

        const string mensagemLinkInvalido = "Link inválido, expirado ou já utilizado. Solicite um novo link.";
        if (string.IsNullOrWhiteSpace(req.Token))
            throw new RegraNegocioException(mensagemLinkInvalido);

        var agora = _relogio.Agora;
        var recuperacao = await _recuperacoes.ObterPorHashAsync(CalcularHash(req.Token.Trim()), ct);
        if (recuperacao is null || !recuperacao.EstaValida(agora))
            throw new RegraNegocioException(mensagemLinkInvalido);

        var usuario = await _usuarios.ObterPorIdAsync(recuperacao.UsuarioId, ct)
                      ?? throw new RegraNegocioException(mensagemLinkInvalido);

        usuario.AlterarSenha(_hash.Gerar(req.NovaSenha), agora);
        recuperacao.Encerrar(agora);
        await _uow.SalvarAsync(ct);

        return new MensagemResponse("Senha redefinida com sucesso. Faça login com a nova senha.");
    }

    internal AutenticacaoResponse Autenticar(Usuario usuario)
    {
        var token = _tokens.Gerar(usuario);
        return new AutenticacaoResponse(token.Token, token.ExpiraEm, UsuarioResponse.De(usuario));
    }

    private static string GerarCodigo() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

    /// <summary>Somente o hash do código é gravado (RNF10).</summary>
    public static string CalcularHash(string codigo) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(codigo)));
}
