namespace MagnesiumCoffee.Api.Dominio;

public class Pedido
{
    public const string Pendente = "pendente";
    public const string Cancelado = "cancelado";

    public int Id { get; private set; }
    public int CafeId { get; private set; }
    public Cafe? Cafe { get; private set; }
    public int UsuarioId { get; private set; }
    public Usuario? Usuario { get; private set; }
    public int Quantidade { get; private set; }
    public decimal PrecoUnitario { get; private set; }
    public decimal Total { get; private set; }
    public string EnderecoEntrega { get; private set; } = "";
    public string Status { get; private set; } = Pendente;
    public DateTime CriadoEm { get; private set; }

    private Pedido() { } // EF Core

    public Pedido(int cafeId, int usuarioId, int quantidade, decimal precoUnitario,
        string enderecoEntrega, DateTime criadoEm)
    {
        if (cafeId <= 0) throw new ArgumentException("Café inválido.", nameof(cafeId));
        if (usuarioId <= 0) throw new ArgumentException("Usuário inválido.", nameof(usuarioId));
        if (quantidade is < 1 or > 50) throw new ArgumentException("Quantidade deve ficar entre 1 e 50.", nameof(quantidade));
        if (precoUnitario <= 0) throw new ArgumentException("Preço unitário deve ser positivo.", nameof(precoUnitario));
        if (string.IsNullOrWhiteSpace(enderecoEntrega))
            throw new ArgumentException("Endereço de entrega é obrigatório.", nameof(enderecoEntrega));

        CafeId = cafeId;
        UsuarioId = usuarioId;
        Quantidade = quantidade;
        PrecoUnitario = precoUnitario;
        Total = quantidade * precoUnitario;
        EnderecoEntrega = enderecoEntrega.Trim();
        Status = Pendente;
        CriadoEm = Datas.ExigirUtc(criadoEm, nameof(criadoEm));
    }

    public bool PertenceA(int usuarioId) => UsuarioId == usuarioId;

    public void Cancelar()
    {
        if (Status == Cancelado) throw new InvalidOperationException("Pedido já cancelado.");
        Status = Cancelado;
    }
}
