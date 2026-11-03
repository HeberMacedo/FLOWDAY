namespace FlowDay.Application.Excecoes;

/// <summary>Credenciais inválidas ou sessão ausente. A mensagem é sempre genérica (RN04).</summary>
public sealed class NaoAutorizadoException : Exception
{
    public NaoAutorizadoException(string mensagem) : base(mensagem) { }
}

/// <summary>Conflito com dado já existente, como e-mail ou nome de área repetido.</summary>
public sealed class ConflitoException : Exception
{
    public string Campo { get; }

    public ConflitoException(string campo, string mensagem) : base(mensagem) => Campo = campo;
}

/// <summary>Login bloqueado após 5 falhas consecutivas (RN04).</summary>
public sealed class ContaBloqueadaException : Exception
{
    public DateTime BloqueadoAte { get; }

    public ContaBloqueadaException(DateTime bloqueadoAte)
        : base("Login bloqueado por 15 minutos após 5 tentativas sem sucesso. Tente novamente mais tarde.")
        => BloqueadoAte = bloqueadoAte;
}
