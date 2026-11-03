using FlowDay.Application.Dtos;
using FlowDay.Application.Excecoes;
using FlowDay.Application.Servicos;
using FlowDay.Domain.Comum;
using FlowDay.Domain.Entidades;
using FlowDay.Domain.Enums;
using FlowDay.Tests.Fakes;

namespace FlowDay.Tests.Servicos;

public class ServicosTestes
{
    [Fact(DisplayName = "RF02: o cadastro cria as cinco áreas padrão")]
    public async Task Cadastro_CriaAreasPadrao()
    {
        var cenario = new Cenario();
        var usuario = await cenario.CadastrarAsync();

        var nomes = cenario.Db.Areas.Where(a => a.UsuarioId == usuario.Id).Select(a => a.Nome).OrderBy(n => n);
        Assert.Equal(new[] { "Estudos", "Geral", "Lazer", "Pessoal", "Trabalho" }, nomes);
    }

    [Fact(DisplayName = "RN02: e-mail repetido, com outras maiúsculas, é recusado")]
    public async Task Cadastro_ComEmailRepetido_LancaConflito()
    {
        var cenario = new Cenario();
        await cenario.CadastrarAsync("maria@email.com");

        await Assert.ThrowsAsync<ConflitoException>(() => cenario.CadastrarAsync("MARIA@Email.com"));
    }

    [Fact(DisplayName = "RN04: mensagem genérica e bloqueio na sexta tentativa, mesmo com senha correta")]
    public async Task Login_AposCincoFalhas_FicaBloqueado()
    {
        var cenario = new Cenario();
        await cenario.CadastrarAsync("maria@email.com", "senhaForte123");
        var servico = cenario.Autenticacao();

        var emailInexistente = await Assert.ThrowsAsync<NaoAutorizadoException>(() =>
            servico.EntrarAsync(new LoginRequest { Email = "ninguem@email.com", Senha = "qualquer1" }));
        var senhaErrada = await Assert.ThrowsAsync<NaoAutorizadoException>(() =>
            servico.EntrarAsync(new LoginRequest { Email = "maria@email.com", Senha = "errada123" }));
        Assert.Equal(emailInexistente.Message, senhaErrada.Message);

        for (var i = 0; i < 4; i++)
            await Assert.ThrowsAsync<NaoAutorizadoException>(() =>
                servico.EntrarAsync(new LoginRequest { Email = "maria@email.com", Senha = "errada123" }));

        await Assert.ThrowsAsync<ContaBloqueadaException>(() =>
            servico.EntrarAsync(new LoginRequest { Email = "maria@email.com", Senha = "senhaForte123" }));

        cenario.Relogio.Avancar(TimeSpan.FromMinutes(16));
        var resposta = await servico.EntrarAsync(new LoginRequest { Email = "maria@email.com", Senha = "senhaForte123" });
        Assert.False(string.IsNullOrEmpty(resposta.Token));
    }

    [Fact(DisplayName = "RN06: link de recuperação é de uso único e o pedido novo invalida o anterior")]
    public async Task RecuperacaoDeSenha_LinkDeUsoUnico()
    {
        var cenario = new Cenario();
        var usuario = await cenario.CadastrarAsync();
        var servico = cenario.Autenticacao();

        var semConta = await servico.SolicitarRecuperacaoAsync(new RecuperarSenhaRequest { Email = "ninguem@email.com" });
        await servico.SolicitarRecuperacaoAsync(new RecuperarSenhaRequest { Email = usuario.Email });
        var comConta = await servico.SolicitarRecuperacaoAsync(new RecuperarSenhaRequest { Email = usuario.Email });
        Assert.Equal(semConta.Mensagem, comConta.Mensagem);
        Assert.Equal(2, cenario.Email.Enviados.Count);

        var primeiroToken = ExtrairToken(cenario.Email.Enviados[0].Corpo);
        var segundoToken = ExtrairToken(cenario.Email.Enviados[1].Corpo);

        await Assert.ThrowsAsync<RegraNegocioException>(() => servico.RedefinirSenhaAsync(new RedefinirSenhaRequest
            { Token = primeiroToken, NovaSenha = "novaSenha456", ConfirmacaoSenha = "novaSenha456" }));

        await servico.RedefinirSenhaAsync(new RedefinirSenhaRequest
            { Token = segundoToken, NovaSenha = "novaSenha456", ConfirmacaoSenha = "novaSenha456" });
        await Assert.ThrowsAsync<RegraNegocioException>(() => servico.RedefinirSenhaAsync(new RedefinirSenhaRequest
            { Token = segundoToken, NovaSenha = "outraSenha789", ConfirmacaoSenha = "outraSenha789" }));

        var login = await servico.EntrarAsync(new LoginRequest { Email = usuario.Email, Senha = "novaSenha456" });
        Assert.False(string.IsNullOrEmpty(login.Token));
    }

    [Fact(DisplayName = "RN08: tarefa só com título fica com prioridade Média e área Geral")]
    public async Task CriarTarefa_SomenteComTitulo_UsaPadroes()
    {
        var cenario = new Cenario();
        await cenario.CadastrarAsync();

        var tarefa = await cenario.TarefaServico().CriarAsync(new CriarTarefaRequest { Titulo = "Pagar a conta de luz" });

        Assert.Equal(StatusTarefa.AFazer, tarefa.Status);
        Assert.Equal(Prioridade.Media, tarefa.Prioridade);
        Assert.Equal(Area.NomeGeral, tarefa.Area!.Nome);
    }

    [Fact(DisplayName = "RN01: tarefa de outro usuário é tratada como inexistente")]
    public async Task Tarefa_DeOutroUsuario_NaoEhEncontrada()
    {
        var cenario = new Cenario();
        await cenario.CadastrarAsync("ana@email.com");
        var tarefaDaAna = await cenario.TarefaServico().CriarAsync(new CriarTarefaRequest { Titulo = "Tarefa da Ana" });

        await cenario.CadastrarAsync("bruno@email.com");
        var servico = cenario.TarefaServico();

        await Assert.ThrowsAsync<NaoEncontradoException>(() => servico.ObterAsync(tarefaDaAna.Id));
        await Assert.ThrowsAsync<NaoEncontradoException>(() => servico.ExcluirAsync(tarefaDaAna.Id));
        var lista = await servico.ListarAsync(new FiltroTarefas(), new OrdenacaoTarefas());
        Assert.Empty(lista.Itens);
    }

    [Fact(DisplayName = "RN18: excluir uma área move suas tarefas para a Geral")]
    public async Task ExcluirArea_MoveTarefasParaGeral()
    {
        var cenario = new Cenario();
        var usuario = await cenario.CadastrarAsync();
        var estudos = cenario.Db.Areas.Single(a => a.UsuarioId == usuario.Id && a.Nome == "Estudos");
        var geral = cenario.Db.Areas.Single(a => a.UsuarioId == usuario.Id && a.EhGeral);

        var tarefas = cenario.TarefaServico();
        await tarefas.CriarAsync(new CriarTarefaRequest { Titulo = "Ler artigo", AreaId = estudos.Id });
        await tarefas.CriarAsync(new CriarTarefaRequest { Titulo = "Fazer resumo", AreaId = estudos.Id });

        await cenario.AreaServico().ExcluirAsync(estudos.Id);

        Assert.DoesNotContain(cenario.Db.Areas, a => a.Id == estudos.Id);
        Assert.All(cenario.Db.Tarefas, t => Assert.Equal(geral.Id, t.AreaId));
        Assert.Equal(2, cenario.Db.Tarefas.Count);
    }

    [Fact(DisplayName = "RN16: nome de área repetido, sem diferenciar maiúsculas, é recusado")]
    public async Task CriarArea_ComNomeRepetido_LancaConflito()
    {
        var cenario = new Cenario();
        await cenario.CadastrarAsync();

        await Assert.ThrowsAsync<ConflitoException>(() =>
            cenario.AreaServico().CriarAsync(new AreaRequest { Nome = "ESTUDOS" }));
    }

    [Fact(DisplayName = "RF25: progresso do checklist com 3 de 4 itens é 75%")]
    public async Task Progresso_TresDeQuatro_SetentaECincoPorCento()
    {
        var cenario = new Cenario();
        await cenario.CadastrarAsync();
        var criada = await cenario.TarefaServico().CriarAsync(new CriarTarefaRequest
        {
            Titulo = "Preparar apresentação",
            Itens = new List<string> { "Slides", "Roteiro", "Ensaio", "Revisão" }
        });

        var tarefa = cenario.Db.Tarefas.Single(t => t.Id == criada.Id);
        foreach (var item in tarefa.Itens.Take(3).ToList())
            tarefa.MarcarItem(item.Id, true, cenario.Relogio.Agora);

        var resposta = await cenario.TarefaServico().ObterAsync(tarefa.Id);
        Assert.Equal(75, resposta.Progresso.Percentual);
    }

    private static string ExtrairToken(string corpoHtml)
    {
        const string marcador = "token=";
        var inicio = corpoHtml.IndexOf(marcador, StringComparison.Ordinal) + marcador.Length;
        var fim = corpoHtml.IndexOf('"', inicio);
        return Uri.UnescapeDataString(corpoHtml[inicio..fim]);
    }
}
