using MagnesiumCoffee.Api.Dados;
using MagnesiumCoffee.Api.Dominio;
using MagnesiumCoffee.Api.Dtos;

namespace MagnesiumCoffee.Tests;

// Dados prontos para os testes de serviço.
internal static class Fabrica
{
    public static readonly DateTime Agora = new(2026, 10, 30, 12, 0, 0, DateTimeKind.Utc);

    public static Cafe Cafe(string nome = "Bourbon Amarelo", int estoque = 10, decimal preco = 50m, string municipio = "Espera Feliz") =>
        new(nome, "Sítio Teste", municipio, "Bourbon Amarelo", "Natural", "media", 1100, 86m, 250,
            preco, estoque, Agora, "Notas de caramelo.", Agora);

    public static Usuario Usuario(string email, string papel = Papeis.Cliente) =>
        new("Usuário " + email, email, "hash-falso", papel, Agora);

    public static CafeEntradaDto CafeDto(DateTimeOffset? dataTorra = null) =>
        new("Arara Lavado", "Sítio Novo", "Irupi", "Arara", "Lavado", "clara", 1200, 87m, 250, 70m, 15,
            dataTorra ?? DateTimeOffset.UtcNow.AddDays(-3), "Floral.");

    public static PedidoEntradaDto PedidoDto(int cafeId, int quantidade) =>
        new(cafeId, quantidade, "Rua das Flores, 100 — Guaçuí/ES");

    public static (Usuario A, Usuario B, Usuario Admin, Cafe Cafe) Popular(AppDbContext db, int estoque = 10)
    {
        var a = Usuario("a@exemplo.com");
        var b = Usuario("b@exemplo.com");
        var admin = Usuario("admin@exemplo.com", Papeis.Admin);
        var cafe = Cafe(estoque: estoque);
        db.AddRange(a, b, admin, cafe);
        db.SaveChanges();
        return (a, b, admin, cafe);
    }
}
