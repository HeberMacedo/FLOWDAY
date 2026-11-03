namespace FlowDay.Domain.Comum;

/// <summary>Base das exceções lançadas pelas regras do sistema.</summary>
public abstract class ExcecaoDominio : Exception
{
    protected ExcecaoDominio(string mensagem) : base(mensagem) { }
}

/// <summary>Dado de entrada fora do formato ou dos limites permitidos.</summary>
public sealed class ValidacaoException : ExcecaoDominio
{
    public IReadOnlyDictionary<string, string[]> Erros { get; }

    public ValidacaoException(string campo, string mensagem) : base(mensagem)
    {
        Erros = new Dictionary<string, string[]> { [campo] = new[] { mensagem } };
    }

    public ValidacaoException(IDictionary<string, string[]> erros)
        : base("Um ou mais campos estão inválidos.")
    {
        Erros = new Dictionary<string, string[]>(erros);
    }
}

/// <summary>Operação válida no formato, mas proibida por uma regra de negócio.</summary>
public sealed class RegraNegocioException : ExcecaoDominio
{
    public RegraNegocioException(string mensagem) : base(mensagem) { }
}

/// <summary>Recurso inexistente ou que pertence a outro usuário (RN01).</summary>
public sealed class NaoEncontradoException : ExcecaoDominio
{
    public NaoEncontradoException(string recurso) : base($"{recurso} não encontrado(a).") { }
}
