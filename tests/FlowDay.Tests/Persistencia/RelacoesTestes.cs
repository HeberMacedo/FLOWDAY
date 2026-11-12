using FlowDay.Domain.Entidades;
using FlowDay.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace FlowDay.Tests.Persistencia;

public class RelacoesTestes
{
    [Fact]
    public void Tarefa_DeveReferenciarAreaDoMesmoUsuario()
    {
        var opcoes = new DbContextOptionsBuilder<FlowDayDbContext>()
            .UseMySql("Server=localhost;Database=flowday;User=flowday;Password=flowday",
                new MySqlServerVersion(new Version(8, 4, 0)))
            .Options;
        using var db = new FlowDayDbContext(opcoes);
        var tarefa = db.Model.FindEntityType(typeof(Tarefa))!;
        var relacaoArea = Assert.Single(tarefa.GetForeignKeys(),
            fk => fk.PrincipalEntityType.ClrType == typeof(Area));

        Assert.Equal(new[] { "AreaId", "UsuarioId" }, relacaoArea.Properties.Select(p => p.Name));
        Assert.Equal(new[] { "Id", "UsuarioId" }, relacaoArea.PrincipalKey.Properties.Select(p => p.Name));
        Assert.Equal(DeleteBehavior.Restrict, relacaoArea.DeleteBehavior);
    }
}
