using FlowDay.Domain.Comum;
using FlowDay.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowDay.Infrastructure.Persistencia.Configuracoes;

// Os Ids são gerados no domínio (Guid), por isso ValueGeneratedNever:
// assim o EF trata itens novos de uma coleção como inclusão, e não como alteração.

public class UsuarioConfiguracao : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("usuarios");
        b.HasKey(u => u.Id);
        b.Property(u => u.Id).ValueGeneratedNever();
        b.Property(u => u.Nome).HasMaxLength(Regras.NomeMax).IsRequired();
        b.Property(u => u.Email).HasMaxLength(Regras.EmailMax).IsRequired();
        b.Property(u => u.EmailNormalizado).HasMaxLength(Regras.EmailMax).IsRequired();
        b.HasIndex(u => u.EmailNormalizado).IsUnique(); // RN02
        b.Property(u => u.SenhaHash).HasMaxLength(100).IsRequired();
        b.Ignore(u => u.CarimboSeguranca);
    }
}

public class AreaConfiguracao : IEntityTypeConfiguration<Area>
{
    public void Configure(EntityTypeBuilder<Area> b)
    {
        b.ToTable("areas");
        b.HasKey(a => a.Id);
        b.Property(a => a.Id).ValueGeneratedNever();
        b.Property(a => a.Nome).HasMaxLength(Area.NomeMax).IsRequired();
        b.Property(a => a.NomeNormalizado).HasMaxLength(Area.NomeMax).IsRequired();
        b.HasIndex(a => new { a.UsuarioId, a.NomeNormalizado }).IsUnique(); // RN16
        b.HasAlternateKey(a => new { a.Id, a.UsuarioId });
        b.HasOne<Usuario>().WithMany().HasForeignKey(a => a.UsuarioId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class TarefaConfiguracao : IEntityTypeConfiguration<Tarefa>
{
    public void Configure(EntityTypeBuilder<Tarefa> b)
    {
        b.ToTable("tarefas");
        b.HasKey(t => t.Id);
        b.Property(t => t.Id).ValueGeneratedNever();
        b.Property(t => t.Titulo).HasMaxLength(Tarefa.TituloMax).IsRequired();
        b.Property(t => t.Descricao).HasMaxLength(Tarefa.DescricaoMax);
        b.Property(t => t.Status).HasConversion<int>();
        b.Property(t => t.Prioridade).HasConversion<int>();
        b.Ignore(t => t.ItensConcluidos);
        b.Ignore(t => t.TotalItens);

        b.HasIndex(t => new { t.UsuarioId, t.Status });
        b.HasIndex(t => new { t.UsuarioId, t.Prazo });

        b.HasOne<Usuario>().WithMany().HasForeignKey(t => t.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        // A tarefa só pode apontar para uma área do mesmo usuário (RN01).
        // Restrict: as tarefas são movidas para a Geral antes de a área ser excluída (RN18).
        b.HasOne<Area>().WithMany()
            .HasForeignKey(t => new { t.AreaId, t.UsuarioId })
            .HasPrincipalKey(a => new { a.Id, a.UsuarioId })
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(t => t.Itens).WithOne().HasForeignKey(i => i.TarefaId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(t => t.Itens).HasField("_itens").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class ItemChecklistConfiguracao : IEntityTypeConfiguration<ItemChecklist>
{
    public void Configure(EntityTypeBuilder<ItemChecklist> b)
    {
        b.ToTable("itens_checklist");
        b.HasKey(i => i.Id);
        b.Property(i => i.Id).ValueGeneratedNever();
        b.Property(i => i.Texto).HasMaxLength(ItemChecklist.TextoMax).IsRequired();
    }
}

public class RecuperacaoSenhaConfiguracao : IEntityTypeConfiguration<RecuperacaoSenha>
{
    public void Configure(EntityTypeBuilder<RecuperacaoSenha> b)
    {
        b.ToTable("recuperacoes_senha");
        b.HasKey(r => r.Id);
        b.Property(r => r.Id).ValueGeneratedNever();
        b.Property(r => r.CodigoHash).HasMaxLength(64).IsRequired();
        b.HasIndex(r => r.CodigoHash).IsUnique();
        b.HasOne<Usuario>().WithMany().HasForeignKey(r => r.UsuarioId).OnDelete(DeleteBehavior.Cascade);
    }
}
