using FlowDay.Domain.Comum;

namespace FlowDay.Domain.Entidades;

public class Usuario
{
    public const int MaximoFalhasLogin = 5;
    public static readonly TimeSpan TempoBloqueio = TimeSpan.FromMinutes(15);

    public Guid Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string EmailNormalizado { get; private set; } = string.Empty;
    public string SenhaHash { get; private set; } = string.Empty;
    public int FalhasLogin { get; private set; }
    public DateTime? BloqueadoAte { get; private set; }

    /// <summary>Usado para invalidar sessões emitidas antes de uma troca de senha (RN05).</summary>
    public DateTime SenhaAlteradaEm { get; private set; }
    public DateTime CriadoEm { get; private set; }

    private Usuario() { }

    public Usuario(string nome, string email, string senhaHash, DateTime agora)
    {
        Id = Guid.NewGuid();
        AlterarNome(nome);
        AlterarEmail(email);
        SenhaHash = senhaHash;
        SenhaAlteradaEm = Regras.TruncarSegundos(agora);
        CriadoEm = Regras.TruncarSegundos(agora);
    }

    /// <summary>Valor gravado no token e comparado a cada requisição.</summary>
    public long CarimboSeguranca => SenhaAlteradaEm.Ticks;

    public void AlterarNome(string nome)
    {
        var erro = Regras.ValidarNome(nome);
        if (erro is not null) throw new ValidacaoException("nome", erro);
        Nome = nome.Trim();
    }

    public void AlterarEmail(string email)
    {
        var erro = Regras.ValidarEmail(email);
        if (erro is not null) throw new ValidacaoException("email", erro);
        Email = email.Trim();
        EmailNormalizado = Regras.NormalizarEmail(email);
    }

    /// <summary>Troca ou redefinição de senha: invalida sessões e zera o bloqueio (RN04, RN05).</summary>
    public void AlterarSenha(string novoHash, DateTime agora)
    {
        SenhaHash = novoHash;
        SenhaAlteradaEm = Regras.TruncarSegundos(agora);
        FalhasLogin = 0;
        BloqueadoAte = null;
    }

    public bool EstaBloqueado(DateTime agora) => BloqueadoAte.HasValue && BloqueadoAte.Value > agora;

    /// <summary>RN04: após 5 falhas consecutivas o login fica bloqueado por 15 minutos.</summary>
    public void RegistrarFalhaLogin(DateTime agora)
    {
        LiberarSeBloqueioTerminou(agora);
        FalhasLogin++;
        if (FalhasLogin >= MaximoFalhasLogin)
        {
            BloqueadoAte = agora.Add(TempoBloqueio);
            FalhasLogin = 0;
        }
    }

    public void RegistrarLoginComSucesso()
    {
        FalhasLogin = 0;
        BloqueadoAte = null;
    }

    private void LiberarSeBloqueioTerminou(DateTime agora)
    {
        if (BloqueadoAte.HasValue && BloqueadoAte.Value <= agora)
        {
            BloqueadoAte = null;
            FalhasLogin = 0;
        }
    }
}
