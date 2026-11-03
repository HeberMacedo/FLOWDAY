using FlowDay.Domain.Enums;

namespace FlowDay.Application.Dtos;

public class CadastroRequest
{
    /// <example>Maria Souza</example>
    public string Nome { get; set; } = string.Empty;
    /// <example>maria@email.com</example>
    public string Email { get; set; } = string.Empty;
    /// <example>senhaForte123</example>
    public string Senha { get; set; } = string.Empty;
    /// <example>senhaForte123</example>
    public string ConfirmacaoSenha { get; set; } = string.Empty;
}

public class LoginRequest
{
    /// <example>maria@email.com</example>
    public string Email { get; set; } = string.Empty;
    /// <example>senhaForte123</example>
    public string Senha { get; set; } = string.Empty;
}

public class RecuperarSenhaRequest
{
    /// <example>maria@email.com</example>
    public string Email { get; set; } = string.Empty;
}

public class RedefinirSenhaRequest
{
    /// <summary>Código recebido no link enviado por e-mail.</summary>
    public string Token { get; set; } = string.Empty;
    /// <example>novaSenha456</example>
    public string NovaSenha { get; set; } = string.Empty;
    /// <example>novaSenha456</example>
    public string ConfirmacaoSenha { get; set; } = string.Empty;
}

public class AtualizarPerfilRequest
{
    /// <example>Maria Souza</example>
    public string Nome { get; set; } = string.Empty;
    /// <example>maria.souza@email.com</example>
    public string Email { get; set; } = string.Empty;
    /// <summary>Obrigatória apenas quando o e-mail for alterado (RN22).</summary>
    public string? SenhaAtual { get; set; }
}

public class AlterarSenhaRequest
{
    public string SenhaAtual { get; set; } = string.Empty;
    public string NovaSenha { get; set; } = string.Empty;
    public string ConfirmacaoSenha { get; set; } = string.Empty;
}

public class ExcluirContaRequest
{
    public string SenhaAtual { get; set; } = string.Empty;
}

public class AreaRequest
{
    /// <example>Academia</example>
    public string Nome { get; set; } = string.Empty;
}

public class CriarTarefaRequest
{
    /// <example>Estudar para a prova de Back-end</example>
    public string Titulo { get; set; } = string.Empty;
    /// <example>Revisar arquitetura limpa e testes automatizados</example>
    public string? Descricao { get; set; }
    /// <summary>Quando omitida, a tarefa vai para a área Geral (RN08).</summary>
    public Guid? AreaId { get; set; }
    /// <summary>Quando omitida, a prioridade é Media (RN08).</summary>
    public Prioridade? Prioridade { get; set; }
    /// <summary>Data no formato AAAA-MM-DD, sem horário (RN13).</summary>
    /// <example>2026-12-01</example>
    public DateOnly? Prazo { get; set; }
    /// <summary>Itens de checklist opcionais criados junto com a tarefa.</summary>
    public List<string>? Itens { get; set; }
}

public class AtualizarTarefaRequest
{
    /// <example>Estudar para a prova de Back-end</example>
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    /// <summary>Quando omitida, a área atual é mantida.</summary>
    public Guid? AreaId { get; set; }
    /// <summary>Quando omitida, a prioridade atual é mantida.</summary>
    public Prioridade? Prioridade { get; set; }
    /// <summary>Enviar null remove o prazo.</summary>
    /// <example>2026-12-01</example>
    public DateOnly? Prazo { get; set; }
}

public class AlterarStatusRequest
{
    /// <example>EmAndamento</example>
    public StatusTarefa Status { get; set; }
}

public class ItemRequest
{
    /// <example>Revisar o capítulo 3</example>
    public string Texto { get; set; } = string.Empty;
}

public class MarcarItemRequest
{
    public bool Concluido { get; set; }
}

/// <summary>Filtros combinados (RN23). Todos são opcionais.</summary>
public class FiltroTarefas
{
    /// <summary>Texto procurado no título e na descrição, sem diferenciar maiúsculas.</summary>
    public string? Busca { get; set; }
    public Guid? AreaId { get; set; }
    public Prioridade? Prioridade { get; set; }
    public StatusTarefa? Status { get; set; }
    /// <summary>Início do período de prazo (inclusive).</summary>
    public DateOnly? PrazoDe { get; set; }
    /// <summary>Fim do período de prazo (inclusive).</summary>
    public DateOnly? PrazoAte { get; set; }
}

public enum CampoOrdenacao
{
    Prazo = 1,
    Prioridade = 2,
    Criacao = 3
}

public class OrdenacaoTarefas
{
    public CampoOrdenacao OrdenarPor { get; set; } = CampoOrdenacao.Criacao;
    public bool Decrescente { get; set; }
}
