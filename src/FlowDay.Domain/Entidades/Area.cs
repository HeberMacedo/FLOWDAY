using FlowDay.Domain.Comum;

namespace FlowDay.Domain.Entidades;

public class Area
{
    public const int NomeMin = 1, NomeMax = 50;
    public const string NomeGeral = "Geral";

    /// <summary>Áreas criadas no cadastro do usuário (RN16).</summary>
    public static readonly string[] NomesPadrao = { "Pessoal", "Trabalho", "Estudos", "Lazer", NomeGeral };

    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string NomeNormalizado { get; private set; } = string.Empty;
    public bool EhGeral { get; private set; }

    private Area() { }

    public Area(Guid usuarioId, string nome, bool ehGeral = false)
    {
        Id = Guid.NewGuid();
        UsuarioId = usuarioId;
        EhGeral = ehGeral;
        AplicarNome(nome);
    }

    public static IReadOnlyList<Area> CriarPadrao(Guid usuarioId) =>
        NomesPadrao.Select(n => new Area(usuarioId, n, n == NomeGeral)).ToList();

    public static string? ValidarNome(string? nome)
    {
        var n = nome?.Trim() ?? string.Empty;
        return n.Length is < NomeMin or > NomeMax
            ? $"O nome da área deve ter de {NomeMin} a {NomeMax} caracteres."
            : null;
    }

    public static string Normalizar(string nome) => nome.Trim().ToLowerInvariant();

    /// <summary>RN17: a área Geral não pode ser renomeada.</summary>
    public void Renomear(string nome)
    {
        GarantirQuePodeSerRenomeada();
        AplicarNome(nome);
    }

    public void GarantirQuePodeSerRenomeada()
    {
        if (EhGeral) throw new RegraNegocioException("A área Geral não pode ser renomeada.");
    }

    /// <summary>RN17: a área Geral não pode ser excluída.</summary>
    public void GarantirQuePodeSerExcluida()
    {
        if (EhGeral) throw new RegraNegocioException("A área Geral não pode ser excluída.");
    }

    private void AplicarNome(string nome)
    {
        var erro = ValidarNome(nome);
        if (erro is not null) throw new ValidacaoException("nome", erro);
        Nome = nome.Trim();
        NomeNormalizado = Normalizar(nome);
    }
}
