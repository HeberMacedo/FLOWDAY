using FlowDay.Application.Abstracoes;
using FlowDay.Application.Dtos;
using FlowDay.Domain.Entidades;
using FlowDay.Domain.Enums;
using FlowDay.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace FlowDay.Infrastructure.Repositorios;

// Todas as consultas filtram pelo usuário dono do dado (RN01). O que é de outro
// usuário simplesmente não é encontrado.

public class UsuarioRepositorio : IUsuarioRepositorio
{
    private readonly FlowDayDbContext _db;
    public UsuarioRepositorio(FlowDayDbContext db) => _db = db;

    public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<Usuario?> ObterPorEmailAsync(string emailNormalizado, CancellationToken ct = default) =>
        _db.Usuarios.FirstOrDefaultAsync(u => u.EmailNormalizado == emailNormalizado, ct);

    public Task<bool> EmailEmUsoAsync(string emailNormalizado, Guid? exceto = null, CancellationToken ct = default) =>
        _db.Usuarios.AnyAsync(u => u.EmailNormalizado == emailNormalizado && (exceto == null || u.Id != exceto), ct);

    public void Adicionar(Usuario usuario) => _db.Usuarios.Add(usuario);

    public async Task RemoverComTodosOsDadosAsync(Guid usuarioId, CancellationToken ct = default)
    {
        // A ordem respeita as chaves estrangeiras. Os itens de checklist saem junto com
        // as tarefas, pela exclusão em cascata definida no banco.
        await using var transacao = await _db.Database.BeginTransactionAsync(ct);
        await _db.Tarefas.Where(t => t.UsuarioId == usuarioId).ExecuteDeleteAsync(ct);
        await _db.Areas.Where(a => a.UsuarioId == usuarioId).ExecuteDeleteAsync(ct);
        await _db.RecuperacoesSenha.Where(r => r.UsuarioId == usuarioId).ExecuteDeleteAsync(ct);
        await _db.Usuarios.Where(u => u.Id == usuarioId).ExecuteDeleteAsync(ct);
        await transacao.CommitAsync(ct);
    }
}

public class AreaRepositorio : IAreaRepositorio
{
    private readonly FlowDayDbContext _db;
    public AreaRepositorio(FlowDayDbContext db) => _db = db;

    public Task<List<Area>> ListarAsync(Guid usuarioId, CancellationToken ct = default) =>
        _db.Areas.Where(a => a.UsuarioId == usuarioId).ToListAsync(ct);

    public Task<Area?> ObterAsync(Guid id, Guid usuarioId, CancellationToken ct = default) =>
        _db.Areas.FirstOrDefaultAsync(a => a.Id == id && a.UsuarioId == usuarioId, ct);

    public Task<Area> ObterGeralAsync(Guid usuarioId, CancellationToken ct = default) =>
        _db.Areas.FirstAsync(a => a.UsuarioId == usuarioId && a.EhGeral, ct);

    public Task<bool> NomeEmUsoAsync(Guid usuarioId, string nomeNormalizado, Guid? exceto = null,
        CancellationToken ct = default) =>
        _db.Areas.AnyAsync(a => a.UsuarioId == usuarioId && a.NomeNormalizado == nomeNormalizado
                                && (exceto == null || a.Id != exceto), ct);

    public void Adicionar(Area area) => _db.Areas.Add(area);
    public void AdicionarVarias(IEnumerable<Area> areas) => _db.Areas.AddRange(areas);
    public void Remover(Area area) => _db.Areas.Remove(area);
}

public class TarefaRepositorio : ITarefaRepositorio
{
    private readonly FlowDayDbContext _db;
    public TarefaRepositorio(FlowDayDbContext db) => _db = db;

    public Task<Tarefa?> ObterAsync(Guid id, Guid usuarioId, CancellationToken ct = default) =>
        _db.Tarefas.Include(t => t.Itens)
            .FirstOrDefaultAsync(t => t.Id == id && t.UsuarioId == usuarioId, ct);

    public async Task<(List<Tarefa> Itens, int Total)> ListarAsync(Guid usuarioId, FiltroTarefas filtro,
        OrdenacaoTarefas ordenacao, int pagina, int tamanho, CancellationToken ct = default)
    {
        var consulta = Filtrar(usuarioId, filtro, usarPeriodo: true);
        var total = await consulta.CountAsync(ct);

        var itens = await Ordenar(consulta, ordenacao)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .Include(t => t.Itens)
            .AsSplitQuery()
            .ToListAsync(ct);

        return (itens, total);
    }

    public Task<List<Tarefa>> ListarPorPeriodoAsync(Guid usuarioId, FiltroTarefas filtro, DateOnly inicio, DateOnly fim,
        CancellationToken ct = default) =>
        Filtrar(usuarioId, filtro, usarPeriodo: false)
            .Where(t => t.Prazo != null && t.Prazo >= inicio && t.Prazo <= fim)
            .OrderBy(t => t.Prazo).ThenByDescending(t => t.Prioridade)
            .Include(t => t.Itens)
            .AsSplitQuery()
            .ToListAsync(ct);

    public Task<List<Tarefa>> ListarPorStatusAsync(Guid usuarioId, FiltroTarefas filtro, CancellationToken ct = default) =>
        Filtrar(usuarioId, filtro, usarPeriodo: true)
            .OrderByDescending(t => t.Prioridade).ThenBy(t => t.Prazo == null).ThenBy(t => t.Prazo)
            .Include(t => t.Itens)
            .AsSplitQuery()
            .ToListAsync(ct);

    /// <summary>Retorna as tarefas rastreadas, pois elas serão movidas de área (RN18).</summary>
    public Task<List<Tarefa>> ListarDaAreaAsync(Guid areaId, Guid usuarioId, CancellationToken ct = default) =>
        _db.Tarefas.Where(t => t.AreaId == areaId && t.UsuarioId == usuarioId).ToListAsync(ct);

    public async Task<ResumoPainel> ObterResumoAsync(Guid usuarioId, DateOnly hoje, DateTime inicioSemanaUtc,
        DateTime fimSemanaUtc, CancellationToken ct = default)
    {
        var doUsuario = _db.Tarefas.AsNoTracking().Where(t => t.UsuarioId == usuarioId);

        var aFazer = await doUsuario.CountAsync(t => t.Status == StatusTarefa.AFazer, ct);
        var emAndamento = await doUsuario.CountAsync(t => t.Status == StatusTarefa.EmAndamento, ct);
        var concluidasNaSemana = await doUsuario.CountAsync(t => t.Status == StatusTarefa.Concluida
            && t.ConcluidaEm >= inicioSemanaUtc && t.ConcluidaEm < fimSemanaUtc, ct);
        var atrasadas = await doUsuario.CountAsync(t => t.Status != StatusTarefa.Concluida
            && t.Prazo != null && t.Prazo < hoje, ct);

        return new ResumoPainel(aFazer, emAndamento, concluidasNaSemana, atrasadas);
    }

    public Task<List<Tarefa>> ListarComPrazoEmAsync(Guid usuarioId, DateOnly data, CancellationToken ct = default) =>
        _db.Tarefas.AsNoTracking()
            .Where(t => t.UsuarioId == usuarioId && t.Prazo == data)
            .OrderBy(t => t.Status).ThenByDescending(t => t.Prioridade)
            .Include(t => t.Itens)
            .AsSplitQuery()
            .ToListAsync(ct);

    public void Adicionar(Tarefa tarefa) => _db.Tarefas.Add(tarefa);
    public void Remover(Tarefa tarefa) => _db.Tarefas.Remove(tarefa);
    public void RegistrarNovoItem(ItemChecklist item) => _db.ItensChecklist.Add(item);

    /// <summary>Filtros combinados: a tarefa precisa atender a todos (RN23).</summary>
    private IQueryable<Tarefa> Filtrar(Guid usuarioId, FiltroTarefas filtro, bool usarPeriodo)
    {
        var consulta = _db.Tarefas.AsNoTracking().Where(t => t.UsuarioId == usuarioId);

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            var termo = filtro.Busca.Trim();
            consulta = consulta.Where(t => t.Titulo.Contains(termo)
                                           || (t.Descricao != null && t.Descricao.Contains(termo)));
        }

        if (filtro.AreaId.HasValue)
            consulta = consulta.Where(t => t.AreaId == filtro.AreaId.Value);
        if (filtro.Prioridade.HasValue)
            consulta = consulta.Where(t => t.Prioridade == filtro.Prioridade.Value);
        if (filtro.Status.HasValue)
            consulta = consulta.Where(t => t.Status == filtro.Status.Value);

        if (usarPeriodo && filtro.PrazoDe.HasValue)
            consulta = consulta.Where(t => t.Prazo != null && t.Prazo >= filtro.PrazoDe.Value);
        if (usarPeriodo && filtro.PrazoAte.HasValue)
            consulta = consulta.Where(t => t.Prazo != null && t.Prazo <= filtro.PrazoAte.Value);

        return consulta;
    }

    private static IQueryable<Tarefa> Ordenar(IQueryable<Tarefa> consulta, OrdenacaoTarefas ordenacao)
    {
        var desc = ordenacao.Decrescente;
        return ordenacao.OrdenarPor switch
        {
            // Tarefas sem prazo ficam sempre no fim da lista.
            CampoOrdenacao.Prazo => desc
                ? consulta.OrderBy(t => t.Prazo == null).ThenByDescending(t => t.Prazo).ThenBy(t => t.Id)
                : consulta.OrderBy(t => t.Prazo == null).ThenBy(t => t.Prazo).ThenBy(t => t.Id),
            CampoOrdenacao.Prioridade => desc
                ? consulta.OrderByDescending(t => t.Prioridade).ThenByDescending(t => t.CriadaEm).ThenBy(t => t.Id)
                : consulta.OrderBy(t => t.Prioridade).ThenByDescending(t => t.CriadaEm).ThenBy(t => t.Id),
            _ => desc
                ? consulta.OrderByDescending(t => t.CriadaEm).ThenBy(t => t.Id)
                : consulta.OrderBy(t => t.CriadaEm).ThenBy(t => t.Id)
        };
    }
}

public class RecuperacaoSenhaRepositorio : IRecuperacaoSenhaRepositorio
{
    private readonly FlowDayDbContext _db;
    public RecuperacaoSenhaRepositorio(FlowDayDbContext db) => _db = db;

    public Task<RecuperacaoSenha?> ObterPorHashAsync(string codigoHash, CancellationToken ct = default) =>
        _db.RecuperacoesSenha.FirstOrDefaultAsync(r => r.CodigoHash == codigoHash, ct);

    public Task<List<RecuperacaoSenha>> ListarAtivasAsync(Guid usuarioId, DateTime agora, CancellationToken ct = default) =>
        _db.RecuperacoesSenha.Where(r => r.UsuarioId == usuarioId && r.UsadoEm == null && r.ExpiraEm > agora)
            .ToListAsync(ct);

    public void Adicionar(RecuperacaoSenha recuperacao) => _db.RecuperacoesSenha.Add(recuperacao);
}
