using FlowDay.Application;
using FlowDay.Application.Abstracoes;
using FlowDay.Application.Dtos;
using FlowDay.Domain.Entidades;
using FlowDay.Domain.Enums;

namespace FlowDay.Tests.Fakes;

// Implementações em memória das portas da aplicação, para testar as regras
// sem banco de dados.

public class RelogioFixo : IRelogio
{
    public DateTime Agora { get; set; } = new(2026, 11, 3, 15, 0, 0, DateTimeKind.Utc);
    public DateOnly Hoje => DateOnly.FromDateTime(Agora.AddHours(-3));
    public DateTime InicioDoDiaEmUtc(DateOnly data) => data.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(3);
    public void Avancar(TimeSpan tempo) => Agora = Agora.Add(tempo);
}

public class HashSenhaFalso : IHashSenha
{
    public string Gerar(string senha) => "hash:" + senha;
    public bool Verificar(string senha, string hash) => hash == "hash:" + senha;
}

public class ServicoTokenFalso : IServicoToken
{
    public TokenGerado Gerar(Usuario usuario) => new($"token-{usuario.Id}", DateTime.UtcNow.AddMinutes(60));
}

public class EnviadorEmailFalso : IEnviadorEmail
{
    public List<(string Para, string Assunto, string Corpo)> Enviados { get; } = new();

    public Task EnviarAsync(string para, string assunto, string corpoHtml, CancellationToken ct = default)
    {
        Enviados.Add((para, assunto, corpoHtml));
        return Task.CompletedTask;
    }
}

public class UsuarioAtualFalso : IUsuarioAtual
{
    public Guid Id { get; set; }
}

public class UnidadeDeTrabalhoFalsa : IUnidadeDeTrabalho
{
    public int VezesSalvo { get; private set; }

    public Task SalvarAsync(CancellationToken ct = default)
    {
        VezesSalvo++;
        return Task.CompletedTask;
    }
}

public class BancoEmMemoria
{
    public List<Usuario> Usuarios { get; } = new();
    public List<Area> Areas { get; } = new();
    public List<Tarefa> Tarefas { get; } = new();
    public List<RecuperacaoSenha> Recuperacoes { get; } = new();
}

public class UsuarioRepositorioFalso : IUsuarioRepositorio
{
    private readonly BancoEmMemoria _db;
    public UsuarioRepositorioFalso(BancoEmMemoria db) => _db = db;

    public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_db.Usuarios.FirstOrDefault(u => u.Id == id));

    public Task<Usuario?> ObterPorEmailAsync(string emailNormalizado, CancellationToken ct = default) =>
        Task.FromResult(_db.Usuarios.FirstOrDefault(u => u.EmailNormalizado == emailNormalizado));

    public Task<bool> EmailEmUsoAsync(string emailNormalizado, Guid? exceto = null, CancellationToken ct = default) =>
        Task.FromResult(_db.Usuarios.Any(u => u.EmailNormalizado == emailNormalizado && u.Id != exceto));

    public void Adicionar(Usuario usuario) => _db.Usuarios.Add(usuario);

    public Task RemoverComTodosOsDadosAsync(Guid usuarioId, CancellationToken ct = default)
    {
        _db.Tarefas.RemoveAll(t => t.UsuarioId == usuarioId);
        _db.Areas.RemoveAll(a => a.UsuarioId == usuarioId);
        _db.Recuperacoes.RemoveAll(r => r.UsuarioId == usuarioId);
        _db.Usuarios.RemoveAll(u => u.Id == usuarioId);
        return Task.CompletedTask;
    }
}

public class AreaRepositorioFalso : IAreaRepositorio
{
    private readonly BancoEmMemoria _db;
    public AreaRepositorioFalso(BancoEmMemoria db) => _db = db;

    public Task<List<Area>> ListarAsync(Guid usuarioId, CancellationToken ct = default) =>
        Task.FromResult(_db.Areas.Where(a => a.UsuarioId == usuarioId).ToList());

    public Task<Area?> ObterAsync(Guid id, Guid usuarioId, CancellationToken ct = default) =>
        Task.FromResult(_db.Areas.FirstOrDefault(a => a.Id == id && a.UsuarioId == usuarioId));

    public Task<Area> ObterGeralAsync(Guid usuarioId, CancellationToken ct = default) =>
        Task.FromResult(_db.Areas.First(a => a.UsuarioId == usuarioId && a.EhGeral));

    public Task<bool> NomeEmUsoAsync(Guid usuarioId, string nomeNormalizado, Guid? exceto = null,
        CancellationToken ct = default) =>
        Task.FromResult(_db.Areas.Any(a => a.UsuarioId == usuarioId && a.NomeNormalizado == nomeNormalizado
                                           && a.Id != exceto));

    public void Adicionar(Area area) => _db.Areas.Add(area);
    public void AdicionarVarias(IEnumerable<Area> areas) => _db.Areas.AddRange(areas);
    public void Remover(Area area) => _db.Areas.Remove(area);
}

public class TarefaRepositorioFalso : ITarefaRepositorio
{
    private readonly BancoEmMemoria _db;
    public TarefaRepositorioFalso(BancoEmMemoria db) => _db = db;

    public Task<Tarefa?> ObterAsync(Guid id, Guid usuarioId, CancellationToken ct = default) =>
        Task.FromResult(_db.Tarefas.FirstOrDefault(t => t.Id == id && t.UsuarioId == usuarioId));

    public Task<(List<Tarefa> Itens, int Total)> ListarAsync(Guid usuarioId, FiltroTarefas filtro,
        OrdenacaoTarefas ordenacao, int pagina, int tamanho, CancellationToken ct = default)
    {
        var todas = Filtrar(usuarioId, filtro).ToList();
        return Task.FromResult((todas.Skip((pagina - 1) * tamanho).Take(tamanho).ToList(), todas.Count));
    }

    public Task<List<Tarefa>> ListarPorPeriodoAsync(Guid usuarioId, FiltroTarefas filtro, DateOnly inicio,
        DateOnly fim, CancellationToken ct = default) =>
        Task.FromResult(Filtrar(usuarioId, filtro)
            .Where(t => t.Prazo.HasValue && t.Prazo.Value >= inicio && t.Prazo.Value <= fim).ToList());

    public Task<List<Tarefa>> ListarPorStatusAsync(Guid usuarioId, FiltroTarefas filtro, CancellationToken ct = default) =>
        Task.FromResult(Filtrar(usuarioId, filtro).ToList());

    public Task<List<Tarefa>> ListarDaAreaAsync(Guid areaId, Guid usuarioId, CancellationToken ct = default) =>
        Task.FromResult(_db.Tarefas.Where(t => t.AreaId == areaId && t.UsuarioId == usuarioId).ToList());

    public Task<ResumoPainel> ObterResumoAsync(Guid usuarioId, DateOnly hoje, DateTime inicioSemanaUtc,
        DateTime fimSemanaUtc, CancellationToken ct = default)
    {
        var t = _db.Tarefas.Where(x => x.UsuarioId == usuarioId).ToList();
        return Task.FromResult(new ResumoPainel(
            t.Count(x => x.Status == StatusTarefa.AFazer),
            t.Count(x => x.Status == StatusTarefa.EmAndamento),
            t.Count(x => x.Status == StatusTarefa.Concluida && x.ConcluidaEm >= inicioSemanaUtc && x.ConcluidaEm < fimSemanaUtc),
            t.Count(x => x.EstaAtrasada(hoje))));
    }

    public Task<List<Tarefa>> ListarComPrazoEmAsync(Guid usuarioId, DateOnly data, CancellationToken ct = default) =>
        Task.FromResult(_db.Tarefas.Where(t => t.UsuarioId == usuarioId && t.Prazo == data).ToList());

    public void Adicionar(Tarefa tarefa) => _db.Tarefas.Add(tarefa);
    public void Remover(Tarefa tarefa) => _db.Tarefas.Remove(tarefa);
    public void RegistrarNovoItem(ItemChecklist item) { }

    private IEnumerable<Tarefa> Filtrar(Guid usuarioId, FiltroTarefas f)
    {
        var q = _db.Tarefas.Where(t => t.UsuarioId == usuarioId);
        if (!string.IsNullOrWhiteSpace(f.Busca))
            q = q.Where(t => t.Titulo.Contains(f.Busca, StringComparison.OrdinalIgnoreCase)
                             || (t.Descricao?.Contains(f.Busca, StringComparison.OrdinalIgnoreCase) ?? false));
        if (f.AreaId.HasValue) q = q.Where(t => t.AreaId == f.AreaId);
        if (f.Prioridade.HasValue) q = q.Where(t => t.Prioridade == f.Prioridade);
        if (f.Status.HasValue) q = q.Where(t => t.Status == f.Status);
        return q;
    }
}

public class RecuperacaoSenhaRepositorioFalso : IRecuperacaoSenhaRepositorio
{
    private readonly BancoEmMemoria _db;
    public RecuperacaoSenhaRepositorioFalso(BancoEmMemoria db) => _db = db;

    public Task<RecuperacaoSenha?> ObterPorHashAsync(string codigoHash, CancellationToken ct = default) =>
        Task.FromResult(_db.Recuperacoes.FirstOrDefault(r => r.CodigoHash == codigoHash));

    public Task<List<RecuperacaoSenha>> ListarAtivasAsync(Guid usuarioId, DateTime agora, CancellationToken ct = default) =>
        Task.FromResult(_db.Recuperacoes.Where(r => r.UsuarioId == usuarioId && r.EstaValida(agora)).ToList());

    public void Adicionar(RecuperacaoSenha recuperacao) => _db.Recuperacoes.Add(recuperacao);
}

/// <summary>Monta os serviços reais da aplicação sobre os fakes.</summary>
public class Cenario
{
    public BancoEmMemoria Db { get; } = new();
    public RelogioFixo Relogio { get; } = new();
    public UsuarioAtualFalso UsuarioAtual { get; } = new();
    public EnviadorEmailFalso Email { get; } = new();
    public UnidadeDeTrabalhoFalsa Uow { get; } = new();
    public HashSenhaFalso Hash { get; } = new();

    public UsuarioRepositorioFalso Usuarios => new(Db);
    public AreaRepositorioFalso Areas => new(Db);
    public TarefaRepositorioFalso Tarefas => new(Db);
    public RecuperacaoSenhaRepositorioFalso Recuperacoes => new(Db);

    public FlowDay.Application.Servicos.AutenticacaoServico Autenticacao() =>
        new(Usuarios, Areas, Recuperacoes, Uow, Hash, new ServicoTokenFalso(), Email, Relogio,
            new OpcoesAplicacao { UrlFrontend = "http://localhost:5500" },
            Microsoft.Extensions.Logging.Abstractions.NullLogger<FlowDay.Application.Servicos.AutenticacaoServico>.Instance);

    public FlowDay.Application.Servicos.TarefaServico TarefaServico() =>
        new(Tarefas, Areas, Uow, UsuarioAtual, Relogio);

    public FlowDay.Application.Servicos.AreaServico AreaServico() =>
        new(Areas, Tarefas, Uow, UsuarioAtual, Relogio);

    /// <summary>Cadastra um usuário pelo fluxo real e o define como usuário da requisição.</summary>
    public async Task<Usuario> CadastrarAsync(string email = "maria@email.com", string senha = "senhaForte123")
    {
        await Autenticacao().CadastrarAsync(new CadastroRequest
        {
            Nome = "Maria Souza",
            Email = email,
            Senha = senha,
            ConfirmacaoSenha = senha
        });
        var usuario = Db.Usuarios.Single(u => u.EmailNormalizado == email.ToLowerInvariant());
        UsuarioAtual.Id = usuario.Id;
        return usuario;
    }
}
