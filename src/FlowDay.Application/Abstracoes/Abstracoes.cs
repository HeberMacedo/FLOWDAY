using FlowDay.Application.Dtos;
using FlowDay.Domain.Entidades;

namespace FlowDay.Application.Abstracoes;

/// <summary>Relógio do sistema. Datas gravadas em UTC; "hoje" no fuso America/Sao_Paulo (P04).</summary>
public interface IRelogio
{
    DateTime Agora { get; }
    DateOnly Hoje { get; }

    /// <summary>Converte a meia-noite de uma data local para UTC.</summary>
    DateTime InicioDoDiaEmUtc(DateOnly data);
}

public interface IHashSenha
{
    string Gerar(string senha);
    bool Verificar(string senha, string hash);
}

public interface IServicoToken
{
    TokenGerado Gerar(Usuario usuario);
}

public record TokenGerado(string Token, DateTime ExpiraEm);

public interface IEnviadorEmail
{
    Task EnviarAsync(string para, string assunto, string corpoHtml, CancellationToken ct = default);
}

/// <summary>Usuário da requisição atual, lido do token.</summary>
public interface IUsuarioAtual
{
    Guid Id { get; }
}

public interface IUnidadeDeTrabalho
{
    Task SalvarAsync(CancellationToken ct = default);
}

public interface IUsuarioRepositorio
{
    Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<Usuario?> ObterPorEmailAsync(string emailNormalizado, CancellationToken ct = default);
    Task<bool> EmailEmUsoAsync(string emailNormalizado, Guid? exceto = null, CancellationToken ct = default);
    void Adicionar(Usuario usuario);

    /// <summary>Remove o usuário e todos os seus dados em uma única transação (RN21).</summary>
    Task RemoverComTodosOsDadosAsync(Guid usuarioId, CancellationToken ct = default);
}

public interface IAreaRepositorio
{
    Task<List<Area>> ListarAsync(Guid usuarioId, CancellationToken ct = default);
    Task<Area?> ObterAsync(Guid id, Guid usuarioId, CancellationToken ct = default);
    Task<Area> ObterGeralAsync(Guid usuarioId, CancellationToken ct = default);
    Task<bool> NomeEmUsoAsync(Guid usuarioId, string nomeNormalizado, Guid? exceto = null, CancellationToken ct = default);
    void Adicionar(Area area);
    void AdicionarVarias(IEnumerable<Area> areas);
    void Remover(Area area);
}

public interface ITarefaRepositorio
{
    Task<Tarefa?> ObterAsync(Guid id, Guid usuarioId, CancellationToken ct = default);
    Task<(List<Tarefa> Itens, int Total)> ListarAsync(Guid usuarioId, FiltroTarefas filtro, OrdenacaoTarefas ordenacao,
                                                      int pagina, int tamanho, CancellationToken ct = default);
    Task<List<Tarefa>> ListarPorPeriodoAsync(Guid usuarioId, FiltroTarefas filtro, DateOnly inicio, DateOnly fim,
                                             CancellationToken ct = default);
    Task<List<Tarefa>> ListarPorStatusAsync(Guid usuarioId, FiltroTarefas filtro, CancellationToken ct = default);
    Task<List<Tarefa>> ListarDaAreaAsync(Guid areaId, Guid usuarioId, CancellationToken ct = default);
    Task<ResumoPainel> ObterResumoAsync(Guid usuarioId, DateOnly hoje, DateTime inicioSemanaUtc, DateTime fimSemanaUtc,
                                        CancellationToken ct = default);
    Task<List<Tarefa>> ListarComPrazoEmAsync(Guid usuarioId, DateOnly data, CancellationToken ct = default);
    void Adicionar(Tarefa tarefa);
    void Remover(Tarefa tarefa);

    /// <summary>Marca explicitamente um item novo para inclusão no banco.</summary>
    void RegistrarNovoItem(ItemChecklist item);
}

public interface IRecuperacaoSenhaRepositorio
{
    Task<RecuperacaoSenha?> ObterPorHashAsync(string codigoHash, CancellationToken ct = default);
    Task<List<RecuperacaoSenha>> ListarAtivasAsync(Guid usuarioId, DateTime agora, CancellationToken ct = default);
    void Adicionar(RecuperacaoSenha recuperacao);
}
