using FlowDay.Domain.Comum;
using FlowDay.Domain.Entidades;

namespace FlowDay.Tests.Dominio;

public class UsuarioEAreaTestes
{
    private static readonly DateTime Agora = new(2026, 11, 3, 15, 0, 0, DateTimeKind.Utc);

    [Theory(DisplayName = "RN03: senha precisa de 8 caracteres, letra e número")]
    [InlineData("curta1", false)]
    [InlineData("somenteletras", false)]
    [InlineData("12345678", false)]
    [InlineData("senhaForte123", true)]
    public void ValidarSenha_AplicaRegra(string senha, bool valida)
    {
        Assert.Equal(valida, Regras.ValidarSenha(senha) is null);
    }

    [Fact(DisplayName = "RN04: 5 falhas seguidas bloqueiam o login por 15 minutos")]
    public void CincoFalhas_BloqueiamPorQuinzeMinutos()
    {
        var usuario = new Usuario("Maria", "maria@email.com", "hash", Agora);

        for (var i = 0; i < 4; i++) usuario.RegistrarFalhaLogin(Agora);
        Assert.False(usuario.EstaBloqueado(Agora));

        usuario.RegistrarFalhaLogin(Agora);
        Assert.True(usuario.EstaBloqueado(Agora.AddMinutes(14)));
        Assert.False(usuario.EstaBloqueado(Agora.AddMinutes(15)));
    }

    [Fact(DisplayName = "RN05: trocar a senha muda o carimbo das sessões")]
    public void AlterarSenha_MudaCarimboDeSeguranca()
    {
        var usuario = new Usuario("Maria", "maria@email.com", "hash", Agora);
        var carimboAntigo = usuario.CarimboSeguranca;

        usuario.AlterarSenha("novoHash", Agora.AddMinutes(5));

        Assert.NotEqual(carimboAntigo, usuario.CarimboSeguranca);
    }

    [Fact(DisplayName = "RN02: e-mail é normalizado sem diferenciar maiúsculas")]
    public void Email_EhNormalizado()
    {
        var usuario = new Usuario("Maria", "  Maria@Email.COM ", "hash", Agora);
        Assert.Equal("maria@email.com", usuario.EmailNormalizado);
    }

    [Fact(DisplayName = "RN17: a área Geral não pode ser renomeada nem excluída")]
    public void AreaGeral_NaoPodeSerRenomeadaNemExcluida()
    {
        var geral = Area.CriarPadrao(Guid.NewGuid()).Single(a => a.EhGeral);

        Assert.Throws<RegraNegocioException>(() => geral.Renomear("Outra"));
        Assert.Throws<RegraNegocioException>(() => geral.GarantirQuePodeSerExcluida());
    }
}
