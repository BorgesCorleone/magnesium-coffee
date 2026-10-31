namespace MagnesiumCoffee.Api.Dominio;

public class Usuario
{
    public int Id { get; private set; }
    public string Nome { get; private set; } = "";
    public string Email { get; private set; } = "";
    public string SenhaHash { get; private set; } = "";
    public string Papel { get; private set; } = Papeis.Cliente;
    public DateTime CriadoEm { get; private set; }

    public List<Pedido> Pedidos { get; private set; } = [];

    private Usuario() { } // EF Core

    public Usuario(string nome, string email, string senhaHash, string papel, DateTime criadoEm)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome é obrigatório.", nameof(nome));
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new ArgumentException("E-mail inválido.", nameof(email));
        if (string.IsNullOrWhiteSpace(senhaHash))
            throw new ArgumentException("Hash da senha é obrigatório.", nameof(senhaHash));
        if (!Papeis.EhValido(papel))
            throw new ArgumentException($"Papel inválido: {papel}.", nameof(papel));

        Nome = nome.Trim();
        Email = email.Trim().ToLowerInvariant();
        SenhaHash = senhaHash;
        Papel = papel;
        CriadoEm = Datas.ExigirUtc(criadoEm, nameof(criadoEm));
    }
}
