using FlowDay.Domain.Comum;

namespace FlowDay.Application.Servicos;

/// <summary>Junta os erros de vários campos para devolvê-los em uma única resposta (UC01 E3).</summary>
internal sealed class Validador
{
    private readonly Dictionary<string, string[]> _erros = new();

    public Validador Campo(string campo, string? erro)
    {
        if (erro is not null) _erros[campo] = new[] { erro };
        return this;
    }

    public void LancarSeHouverErros()
    {
        if (_erros.Count > 0) throw new ValidacaoException(_erros);
    }
}
