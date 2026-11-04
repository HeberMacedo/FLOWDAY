using FlowDay.Domain.Comum;
using FlowDay.Domain.Enums;

namespace FlowDay.Domain.Entidades;

public class Tarefa
{
    public const int TituloMin = 3, TituloMax = 120, DescricaoMax = 1000, MaximoItens = 50;

    private readonly List<ItemChecklist> _itens = new();

    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public Guid AreaId { get; private set; }
    public string Titulo { get; private set; } = string.Empty;
    public string? Descricao { get; private set; }
    public StatusTarefa Status { get; private set; }
    public Prioridade Prioridade { get; private set; }
    public DateOnly? Prazo { get; private set; }
    public DateTime? ConcluidaEm { get; private set; }
    public DateTime CriadaEm { get; private set; }
    public DateTime AtualizadaEm { get; private set; }

    public IReadOnlyCollection<ItemChecklist> Itens => _itens.AsReadOnly();

    private Tarefa() { }

    /// <summary>RN08: toda nova tarefa nasce com status A fazer.</summary>
    public Tarefa(Guid usuarioId, Guid areaId, string titulo, string? descricao,
                  Prioridade prioridade, DateOnly? prazo, DateOnly hoje, DateTime agora)
    {
        Id = Guid.NewGuid();
        UsuarioId = usuarioId;
        AreaId = areaId;
        DefinirTitulo(titulo);
        DefinirDescricao(descricao);
        DefinirPrioridade(prioridade);
        ValidarNovoPrazo(prazo, hoje);
        Prazo = prazo;
        Status = StatusTarefa.AFazer;
        CriadaEm = agora;
        AtualizadaEm = agora;
    }

    public static string? ValidarTitulo(string? titulo)
    {
        var t = titulo?.Trim() ?? string.Empty;
        return t.Length is < TituloMin or > TituloMax
            ? $"O título deve ter de {TituloMin} a {TituloMax} caracteres."
            : null;
    }

    public static string? ValidarDescricao(string? descricao) =>
        descricao is not null && descricao.Trim().Length > DescricaoMax
            ? $"A descrição deve ter no máximo {DescricaoMax} caracteres."
            : null;

    /// <summary>
    /// Altera os dados da tarefa. O prazo só é validado quando muda, para permitir
    /// editar outros campos de uma tarefa já vencida (RN13).
    /// </summary>
    public void Atualizar(string titulo, string? descricao, Guid areaId, Prioridade prioridade,
                          DateOnly? prazo, DateOnly hoje, DateTime agora)
    {
        DefinirTitulo(titulo);
        DefinirDescricao(descricao);
        DefinirPrioridade(prioridade);
        if (prazo != Prazo)
        {
            ValidarNovoPrazo(prazo, hoje);
            Prazo = prazo;
        }
        AreaId = areaId;
        AtualizadaEm = agora;
    }

    /// <summary>RN18: usada quando a área da tarefa é excluída.</summary>
    public void MoverParaArea(Guid areaId, DateTime agora)
    {
        AreaId = areaId;
        AtualizadaEm = agora;
    }

    /// <summary>RN09, RN10 e RN11.</summary>
    public void AlterarStatus(StatusTarefa novoStatus, DateTime agora)
    {
        if (!Enum.IsDefined(novoStatus))
            throw new ValidacaoException("status", "Status inválido.");
        if (novoStatus == Status) return;

        if (novoStatus == StatusTarefa.Concluida)
        {
            if (_itens.Any(i => !i.Concluido))
                throw new RegraNegocioException(
                    "A tarefa só pode ser concluída quando todos os itens do checklist estiverem concluídos.");
            ConcluidaEm = agora;
        }
        else
        {
            ConcluidaEm = null;
        }

        Status = novoStatus;
        AtualizadaEm = agora;
    }

    /// <summary>RN14: prazo anterior à data atual e tarefa não concluída.</summary>
    public bool EstaAtrasada(DateOnly hoje) =>
        Prazo.HasValue && Prazo.Value < hoje && Status != StatusTarefa.Concluida;

    public int ItensConcluidos => _itens.Count(i => i.Concluido);
    public int TotalItens => _itens.Count;

    public ItemChecklist AdicionarItem(string texto, DateTime agora)
    {
        GarantirQueChecklistPodeSerAlterado();
        if (_itens.Count >= MaximoItens)
            throw new RegraNegocioException($"A tarefa já possui o limite de {MaximoItens} itens de checklist.");

        var ordem = _itens.Count == 0 ? 1 : _itens.Max(i => i.Ordem) + 1;
        var item = new ItemChecklist(Id, texto, ordem);
        _itens.Add(item);
        AtualizadaEm = agora;
        return item;
    }

    public ItemChecklist EditarItem(Guid itemId, string texto, DateTime agora)
    {
        GarantirQueChecklistPodeSerAlterado();
        var item = ObterItem(itemId);
        item.DefinirTexto(texto);
        AtualizadaEm = agora;
        return item;
    }

    /// <summary>RN10: marcar o último item não conclui a tarefa automaticamente.</summary>
    public ItemChecklist MarcarItem(Guid itemId, bool concluido, DateTime agora)
    {
        GarantirQueChecklistPodeSerAlterado();
        var item = ObterItem(itemId);
        item.Marcar(concluido);
        AtualizadaEm = agora;
        return item;
    }

    public void RemoverItem(Guid itemId, DateTime agora)
    {
        GarantirQueChecklistPodeSerAlterado();
        _itens.Remove(ObterItem(itemId));
        AtualizadaEm = agora;
    }

    private ItemChecklist ObterItem(Guid itemId) =>
        _itens.FirstOrDefault(i => i.Id == itemId) ?? throw new NaoEncontradoException("Item de checklist não encontrado.");

    /// <summary>RN12: itens de tarefa concluída não podem ser alterados.</summary>
    private void GarantirQueChecklistPodeSerAlterado()
    {
        if (Status == StatusTarefa.Concluida)
            throw new RegraNegocioException(
                "A tarefa está concluída. Reabra a tarefa para alterar o checklist.");
    }

    private void DefinirTitulo(string titulo)
    {
        var erro = ValidarTitulo(titulo);
        if (erro is not null) throw new ValidacaoException("titulo", erro);
        Titulo = titulo.Trim();
    }

    private void DefinirDescricao(string? descricao)
    {
        var erro = ValidarDescricao(descricao);
        if (erro is not null) throw new ValidacaoException("descricao", erro);
        Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim();
    }

    private void DefinirPrioridade(Prioridade prioridade)
    {
        if (!Enum.IsDefined(prioridade))
            throw new ValidacaoException("prioridade", "Prioridade inválida.");
        Prioridade = prioridade;
    }

    private static void ValidarNovoPrazo(DateOnly? prazo, DateOnly hoje)
    {
        if (prazo.HasValue && prazo.Value < hoje)
            throw new ValidacaoException("prazo", "O prazo não pode ser anterior à data atual.");
    }
}
