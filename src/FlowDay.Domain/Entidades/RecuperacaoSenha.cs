namespace FlowDay.Domain.Entidades;

/// <summary>Pedido de recuperação de senha (RN06). Só o hash do código é guardado.</summary>
public class RecuperacaoSenha
{
    public static readonly TimeSpan Validade = TimeSpan.FromMinutes(30);

    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string CodigoHash { get; private set; } = string.Empty;
    public DateTime CriadoEm { get; private set; }
    public DateTime ExpiraEm { get; private set; }
    public DateTime? UsadoEm { get; private set; }

    private RecuperacaoSenha() { }

    public RecuperacaoSenha(Guid usuarioId, string codigoHash, DateTime agora)
    {
        Id = Guid.NewGuid();
        UsuarioId = usuarioId;
        CodigoHash = codigoHash;
        CriadoEm = agora;
        ExpiraEm = agora.Add(Validade);
    }

    public bool EstaValida(DateTime agora) => UsadoEm is null && ExpiraEm > agora;

    /// <summary>Usado ao redefinir a senha e também para invalidar pedidos anteriores.</summary>
    public void Encerrar(DateTime agora) => UsadoEm ??= agora;
}
