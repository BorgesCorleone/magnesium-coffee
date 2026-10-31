using MagnesiumCoffee.Api.Dominio;

namespace MagnesiumCoffee.Tests;

public class DominioTests
{
    private static readonly DateTime Agora = new(2026, 10, 30, 12, 0, 0, DateTimeKind.Utc);

    private static Cafe NovoCafe(int estoque = 10, DateTime? dataTorra = null) =>
        new("Teste", "Produtor", "Alto Caparaó", "Catuaí", "Natural", "media", 1200, 85m, 250,
            50m, estoque, dataTorra ?? Agora, "desc", Agora);

    [Fact]
    public void Cafe_rejeita_data_que_nao_e_utc()
    {
        var local = new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Local);
        Assert.Throws<ArgumentException>(() => NovoCafe(dataTorra: local));
    }

    [Fact]
    public void Cafe_tem_estoque_ate_o_limite_exato()
    {
        var cafe = NovoCafe(estoque: 10);
        Assert.True(cafe.TemEstoque(10));
        Assert.False(cafe.TemEstoque(0));
    }

    [Fact]
    public void Cafe_rejeita_torra_invalida()
    {
        Assert.Throws<ArgumentException>(() =>
            new Cafe("Teste", "Produtor", "Iúna", "Catuaí", "Natural", "extra", 1000, 85m, 250, 50m, 1, Agora, "", Agora));
    }

    [Fact]
    public void Cafe_nao_tem_estoque_para_quantidade_maior()
    {
        var cafe = NovoCafe(estoque: 2);
        Assert.False(cafe.TemEstoque(3));
    }

    [Fact]
    public void Pedido_calcula_total()
    {
        var pedido = new Pedido(1, 1, 3, 54.90m, "Rua das Flores, 10", Agora);
        Assert.Equal(164.70m, pedido.Total);
        Assert.Equal(Pedido.Pendente, pedido.Status);
    }
}
