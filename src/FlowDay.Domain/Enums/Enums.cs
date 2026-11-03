namespace FlowDay.Domain.Enums;

/// <summary>Status da tarefa, equivalentes às colunas do Kanban (RN09).</summary>
public enum StatusTarefa
{
    AFazer = 1,
    EmAndamento = 2,
    Concluida = 3
}

/// <summary>Prioridade da tarefa (RN15).</summary>
public enum Prioridade
{
    Baixa = 1,
    Media = 2,
    Alta = 3,
    Urgente = 4
}

public static class PrioridadeExtensoes
{
    public static string Rotulo(this Prioridade p) => p switch
    {
        Prioridade.Baixa => "Baixa",
        Prioridade.Media => "Média",
        Prioridade.Alta => "Alta",
        Prioridade.Urgente => "Urgente",
        _ => p.ToString()
    };

    /// <summary>Cor de referência para o front-end: verde, amarelo, laranja e vermelho.</summary>
    public static string Cor(this Prioridade p) => p switch
    {
        Prioridade.Baixa => "#22C55E",
        Prioridade.Media => "#EAB308",
        Prioridade.Alta => "#F97316",
        Prioridade.Urgente => "#EF4444",
        _ => "#6B7280"
    };
}

public static class StatusExtensoes
{
    public static string Rotulo(this StatusTarefa s) => s switch
    {
        StatusTarefa.AFazer => "A fazer",
        StatusTarefa.EmAndamento => "Em andamento",
        StatusTarefa.Concluida => "Concluída",
        _ => s.ToString()
    };
}
