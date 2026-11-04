using FlowDay.Application.Abstracoes;
using FlowDay.Application.Dtos;
using FlowDay.Application.Excecoes;
using FlowDay.Domain.Comum;
using FlowDay.Domain.Entidades;

namespace FlowDay.Application.Servicos;

/// <summary>UC10: consulta e alteração do perfil, troca de senha e exclusão da conta.</summary>
public class PerfilServico
{
    private readonly IUsuarioRepositorio _usuarios;
    private readonly IUnidadeDeTrabalho _uow;
    private readonly IHashSenha _hash;
    private readonly AutenticacaoServico _autenticacao;
    private readonly IUsuarioAtual _atual;
    private readonly IRelogio _relogio;

    public PerfilServico(IUsuarioRepositorio usuarios, IUnidadeDeTrabalho uow, IHashSenha hash,
        AutenticacaoServico autenticacao, IUsuarioAtual atual, IRelogio relogio)
    {
        _usuarios = usuarios;
        _uow = uow;
        _hash = hash;
        _autenticacao = autenticacao;
        _atual = atual;
        _relogio = relogio;
    }

    public async Task<UsuarioResponse> ObterAsync(CancellationToken ct = default) =>
        UsuarioResponse.De(await ObterUsuarioAsync(ct));

    /// <summary>RF09: alterar o e-mail exige a senha atual (RN22).</summary>
    public async Task<UsuarioResponse> AtualizarAsync(AtualizarPerfilRequest req, CancellationToken ct = default)
    {
        new Validador()
            .Campo("nome", Regras.ValidarNome(req.Nome))
            .Campo("email", Regras.ValidarEmail(req.Email))
            .LancarSeHouverErros();

        var usuario = await ObterUsuarioAsync(ct);
        var novoEmail = Regras.NormalizarEmail(req.Email);

        if (novoEmail != usuario.EmailNormalizado)
        {
            ConferirSenhaAtual(usuario, req.SenhaAtual);
            if (await _usuarios.EmailEmUsoAsync(novoEmail, usuario.Id, ct))
                throw new ConflitoException("email", "Este e-mail já está em uso.");
        }

        usuario.AlterarNome(req.Nome);
        usuario.AlterarEmail(req.Email);
        await _uow.SalvarAsync(ct);
        return UsuarioResponse.De(usuario);
    }

    /// <summary>
    /// RF10: troca a senha e invalida todas as sessões anteriores (RN05).
    /// Devolve um novo token para que o aparelho atual continue conectado.
    /// </summary>
    public async Task<AutenticacaoResponse> AlterarSenhaAsync(AlterarSenhaRequest req, CancellationToken ct = default)
    {
        var usuario = await ObterUsuarioAsync(ct);
        ConferirSenhaAtual(usuario, req.SenhaAtual);

        new Validador()
            .Campo("novaSenha", Regras.ValidarSenha(req.NovaSenha))
            .Campo("confirmacaoSenha", req.NovaSenha != req.ConfirmacaoSenha ? "A confirmação de senha não confere." : null)
            .LancarSeHouverErros();

        usuario.AlterarSenha(_hash.Gerar(req.NovaSenha), _relogio.Agora);
        await _uow.SalvarAsync(ct);
        return _autenticacao.Autenticar(usuario);
    }

    /// <summary>RF11: exige a senha atual e remove definitivamente todos os dados (RN21).</summary>
    public async Task ExcluirContaAsync(ExcluirContaRequest req, CancellationToken ct = default)
    {
        var usuario = await ObterUsuarioAsync(ct);
        ConferirSenhaAtual(usuario, req.SenhaAtual);
        await _usuarios.RemoverComTodosOsDadosAsync(usuario.Id, ct);
    }

    private void ConferirSenhaAtual(Usuario usuario, string? senhaAtual)
    {
        if (string.IsNullOrEmpty(senhaAtual) || !_hash.Verificar(senhaAtual, usuario.SenhaHash))
            throw new ValidacaoException("senhaAtual", "A senha atual está incorreta.");
    }

    private async Task<Usuario> ObterUsuarioAsync(CancellationToken ct) =>
        await _usuarios.ObterPorIdAsync(_atual.Id, ct) ?? throw new NaoEncontradoException("Usuário não encontrado.");
}
