using FlowDay.Domain.Entidades;
using FlowDay.Domain.Enums;

namespace FlowDay.Application.Dtos;

public record UsuarioResponse(Guid Id, string Nome, string Email, DateTime CriadoEm)
{
    public static UsuarioResponse De(Usuario u) => new(u.Id, u.Nome, u.Email, u.CriadoEm);
}

public record AutenticacaoResponse(string Token, DateTime ExpiraEm, UsuarioResponse Usuario);

public record MensagemResponse(string Mensagem);

public record AreaResponse(Guid Id, string Nome, bool EhGeral)
{
    public static AreaResponse De(Area a) => new(a.Id, a.Nome, a.EhGeral);
}

public record ItemChecklistResponse(Guid Id, string Texto, bool Concluido, int Ordem)
{
    public static ItemChecklistResponse De(ItemChecklist i) => new(i.Id, i.Texto, i.Concluido, i.Ordem);
}

public record ProgressoResponse(int Concluidos, int Total, int Percentual);

public record TarefaResponse(
    Guid Id,
    string Titulo,
    string? Descricao,
    StatusTarefa Status,
    string StatusRotulo,
    Prioridade Prioridade,
    string PrioridadeRotulo,
    string PrioridadeCor,
    DateOnly? Prazo,
    bool Atrasada,
    DateTime? ConcluidaEm,
    DateTime CriadaEm,
    DateTime AtualizadaEm,
    AreaResponse? Area,
    ProgressoResponse Progresso,
    IReadOnlyList<ItemChecklistResponse> Itens)
{
    public static TarefaResponse De(Tarefa t, DateOnly hoje, Area? area)
    {
        var total = t.TotalItens;
        var concluidos = t.ItensConcluidos;
        var percentual = total == 0 ? 0 : (int)Math.Round(concluidos * 100.0 / total);

        return new TarefaResponse(
            t.Id, t.Titulo, t.Descricao,
            t.Status, t.Status.Rotulo(),
            t.Prioridade, t.Prioridade.Rotulo(), t.Prioridade.Cor(),
            t.Prazo, t.EstaAtrasada(hoje), t.ConcluidaEm, t.CriadaEm, t.AtualizadaEm,
            area is null ? null : AreaResponse.De(area),
            new ProgressoResponse(concluidos, total, percentual),
            t.Itens.OrderBy(i => i.Ordem).Select(ItemChecklistResponse.De).ToList());
    }
}

public record PaginaResponse<T>(IReadOnlyList<T> Itens, int Pagina, int TamanhoPagina, int Total, int TotalPaginas);

public record ColunaKanbanResponse(StatusTarefa Status, string Rotulo, int Total, IReadOnlyList<TarefaResponse> Tarefas);

public record KanbanResponse(IReadOnlyList<ColunaKanbanResponse> Colunas);

public record CalendarioResponse(DateOnly Inicio, DateOnly Fim, IReadOnlyList<TarefaResponse> Tarefas);

public record ResumoPainel(int AFazer, int EmAndamento, int ConcluidasNaSemana, int Atrasadas);

public record PainelResponse(
    int AFazer,
    int EmAndamento,
    int ConcluidasNaSemana,
    int Atrasadas,
    IReadOnlyList<TarefaResponse> TarefasDeHoje);
