using FlowDay.Application.Abstracoes;
using FlowDay.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FlowDay.Infrastructure.Persistencia;

public class FlowDayDbContext : DbContext, IUnidadeDeTrabalho
{
    public FlowDayDbContext(DbContextOptions<FlowDayDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Tarefa> Tarefas => Set<Tarefa>();
    public DbSet<ItemChecklist> ItensChecklist => Set<ItemChecklist>();
    public DbSet<RecuperacaoSenha> RecuperacoesSenha => Set<RecuperacaoSenha>();

    public Task SalvarAsync(CancellationToken ct = default) => SaveChangesAsync(ct);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // A collation padrão do MySQL 8 (utf8mb4_0900_ai_ci) não diferencia maiúsculas
        // de minúsculas, o que atende à busca da RN23.
        modelBuilder.HasCharSet("utf8mb4");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FlowDayDbContext).Assembly);

        // Datas são gravadas em UTC; ao ler do MySQL, o valor volta marcado como UTC.
        var utc = new ConversorUtc();
        var utcNulo = new ConversorUtcNulo();
        foreach (var entidade in modelBuilder.Model.GetEntityTypes())
        foreach (var propriedade in entidade.GetProperties())
        {
            if (propriedade.ClrType == typeof(DateTime)) propriedade.SetValueConverter(utc);
            else if (propriedade.ClrType == typeof(DateTime?)) propriedade.SetValueConverter(utcNulo);
        }
    }

    private sealed class ConversorUtc : ValueConverter<DateTime, DateTime>
    {
        public ConversorUtc() : base(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc)) { }
    }

    private sealed class ConversorUtcNulo : ValueConverter<DateTime?, DateTime?>
    {
        public ConversorUtcNulo()
            : base(v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v) { }
    }
}
