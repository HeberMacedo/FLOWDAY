using FlowDay.Domain.Comum;

namespace FlowDay.Domain.Entidades;

public class ItemChecklist
{
    public const int TextoMin = 1, TextoMax = 200;

    public Guid Id { get; private set; }
    public Guid TarefaId { get; private set; }
    public string Texto { get; private set; } = string.Empty;
    public bool Concluido { get; private set; }
    public int Ordem { get; private set; }

    private ItemChecklist() { }

    internal ItemChecklist(Guid tarefaId, string texto, int ordem)
    {
        Id = Guid.NewGuid();
        TarefaId = tarefaId;
        Ordem = ordem;
        DefinirTexto(texto);
    }

    internal void DefinirTexto(string texto)
    {
        var t = texto?.Trim() ?? string.Empty;
        if (t.Length is < TextoMin or > TextoMax)
            throw new ValidacaoException("texto", $"O texto do item deve ter de {TextoMin} a {TextoMax} caracteres.");
        Texto = t;
    }

    internal void Marcar(bool concluido) => Concluido = concluido;
}
