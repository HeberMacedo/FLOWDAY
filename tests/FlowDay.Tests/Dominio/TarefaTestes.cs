using FlowDay.Domain.Comum;
using FlowDay.Domain.Entidades;
using FlowDay.Domain.Enums;

namespace FlowDay.Tests.Dominio;

public class TarefaTestes
{
    private static readonly DateOnly Hoje = new(2026, 11, 3);
    private static readonly DateTime Agora = new(2026, 11, 3, 15, 0, 0, DateTimeKind.Utc);

    private static Tarefa NovaTarefa(DateOnly? prazo = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Estudar para a prova", null, Prioridade.Media, prazo, Hoje, Agora);

    [Fact(DisplayName = "RN08: nova tarefa nasce com status A fazer")]
    public void NovaTarefa_NasceComStatusAFazer()
    {
        var tarefa = NovaTarefa();

        Assert.Equal(StatusTarefa.AFazer, tarefa.Status);
        Assert.Null(tarefa.ConcluidaEm);
    }

    [Fact(DisplayName = "RN10: não conclui tarefa com item de checklist pendente")]
    public void Concluir_ComItemPendente_LancaRegraDeNegocio()
    {
        var tarefa = NovaTarefa();
        tarefa.AdicionarItem("Revisar o capítulo 1", Agora);
        tarefa.AdicionarItem("Fazer exercícios", Agora);

        Assert.Throws<RegraNegocioException>(() => tarefa.AlterarStatus(StatusTarefa.Concluida, Agora));
        Assert.Equal(StatusTarefa.AFazer, tarefa.Status);
    }

    [Fact(DisplayName = "RN10 e RN11: tarefa sem itens é concluída e registra data e hora")]
    public void Concluir_SemItens_RegistraDataDeConclusao()
    {
        var tarefa = NovaTarefa();

        tarefa.AlterarStatus(StatusTarefa.Concluida, Agora);

        Assert.Equal(StatusTarefa.Concluida, tarefa.Status);
        Assert.Equal(Agora, tarefa.ConcluidaEm);
    }

    [Fact(DisplayName = "RN10: marcar o último item não conclui a tarefa automaticamente")]
    public void MarcarUltimoItem_NaoConcluiTarefa()
    {
        var tarefa = NovaTarefa();
        var item = tarefa.AdicionarItem("Único passo", Agora);

        tarefa.MarcarItem(item.Id, true, Agora);

        Assert.Equal(StatusTarefa.AFazer, tarefa.Status);
        Assert.Equal(1, tarefa.ItensConcluidos);
    }

    [Fact(DisplayName = "RN11: reabrir a tarefa remove a data de conclusão")]
    public void Reabrir_RemoveDataDeConclusao()
    {
        var tarefa = NovaTarefa();
        tarefa.AlterarStatus(StatusTarefa.Concluida, Agora);

        tarefa.AlterarStatus(StatusTarefa.EmAndamento, Agora.AddHours(1));

        Assert.Equal(StatusTarefa.EmAndamento, tarefa.Status);
        Assert.Null(tarefa.ConcluidaEm);
    }

    [Fact(DisplayName = "RN12: itens de tarefa concluída não podem ser alterados")]
    public void TarefaConcluida_NaoAceitaAlteracaoNoChecklist()
    {
        var tarefa = NovaTarefa();
        var item = tarefa.AdicionarItem("Passo 1", Agora);
        tarefa.MarcarItem(item.Id, true, Agora);
        tarefa.AlterarStatus(StatusTarefa.Concluida, Agora);

        Assert.Throws<RegraNegocioException>(() => tarefa.AdicionarItem("Passo 2", Agora));
        Assert.Throws<RegraNegocioException>(() => tarefa.EditarItem(item.Id, "Outro texto", Agora));
        Assert.Throws<RegraNegocioException>(() => tarefa.MarcarItem(item.Id, false, Agora));
        Assert.Throws<RegraNegocioException>(() => tarefa.RemoverItem(item.Id, Agora));
    }

    [Fact(DisplayName = "RN13: prazo não pode ser anterior à data atual")]
    public void Criar_ComPrazoNoPassado_LancaValidacao()
    {
        var erro = Assert.Throws<ValidacaoException>(() => NovaTarefa(Hoje.AddDays(-1)));
        Assert.True(erro.Erros.ContainsKey("prazo"));
    }

    [Fact(DisplayName = "RN13: editar o título de tarefa com prazo vencido é permitido")]
    public void Editar_TarefaVencida_SemMudarPrazo_EhPermitido()
    {
        var tarefa = NovaTarefa(Hoje);
        var amanha = Hoje.AddDays(1);

        tarefa.Atualizar("Novo título da tarefa", null, tarefa.AreaId, Prioridade.Alta, Hoje, amanha, Agora.AddDays(1));

        Assert.Equal("Novo título da tarefa", tarefa.Titulo);
        Assert.Throws<ValidacaoException>(() =>
            tarefa.Atualizar("Novo título da tarefa", null, tarefa.AreaId, Prioridade.Alta, Hoje.AddDays(-5), amanha, Agora));
    }

    [Fact(DisplayName = "RN14: atrasada é a tarefa não concluída com prazo anterior a hoje")]
    public void EstaAtrasada_ConsideraPrazoEStatus()
    {
        var tarefa = NovaTarefa(Hoje);
        var depois = Hoje.AddDays(2);

        Assert.False(tarefa.EstaAtrasada(Hoje));
        Assert.True(tarefa.EstaAtrasada(depois));

        tarefa.AlterarStatus(StatusTarefa.Concluida, Agora);
        Assert.False(tarefa.EstaAtrasada(depois));
    }

    [Fact(DisplayName = "RN19: limite de 50 itens e texto de 1 a 200 caracteres")]
    public void Checklist_RespeitaLimites()
    {
        var tarefa = NovaTarefa();
        for (var i = 1; i <= Tarefa.MaximoItens; i++)
            tarefa.AdicionarItem($"Item {i}", Agora);

        Assert.Throws<RegraNegocioException>(() => tarefa.AdicionarItem("Item 51", Agora));

        var outra = NovaTarefa();
        Assert.Throws<ValidacaoException>(() => outra.AdicionarItem("   ", Agora));
        Assert.Throws<ValidacaoException>(() => outra.AdicionarItem(new string('a', 201), Agora));
    }

    [Theory(DisplayName = "RN07: título deve ter de 3 a 120 caracteres")]
    [InlineData("ab")]
    [InlineData("")]
    public void Criar_ComTituloInvalido_LancaValidacao(string titulo)
    {
        Assert.Throws<ValidacaoException>(() =>
            new Tarefa(Guid.NewGuid(), Guid.NewGuid(), titulo, null, Prioridade.Media, null, Hoje, Agora));
        Assert.NotNull(Tarefa.ValidarTitulo(new string('x', 121)));
    }
}
