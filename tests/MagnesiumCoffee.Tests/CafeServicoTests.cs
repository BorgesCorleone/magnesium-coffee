using MagnesiumCoffee.Api.Dominio;
using MagnesiumCoffee.Api.Servicos;
using Microsoft.Extensions.Logging.Abstractions;

namespace MagnesiumCoffee.Tests;

public class CafeServicoTests : IDisposable
{
    private readonly BancoDeTeste _banco = new();

    private CafeServico CriarServico() => new(_banco.CriarContexto(), NullLogger<CafeServico>.Instance);

    private void Inserir(params Cafe[] cafes)
    {
        using var db = _banco.CriarContexto();
        db.Cafes.AddRange(cafes);
        db.SaveChanges();
    }

    [Fact]
    public async Task Listagem_pagina_e_informa_total()
    {
        Inserir(Fabrica.Cafe("A"), Fabrica.Cafe("B"), Fabrica.Cafe("C"));

        var pagina = await CriarServico().ListarAsync(null, pagina: 2, tamanho: 2);

        Assert.Single(pagina.Itens);
        Assert.Equal("C", pagina.Itens[0].Nome);
        Assert.Equal(3, pagina.Total);
        Assert.Equal(2, pagina.TotalPaginas);
    }

    [Fact]
    public async Task Busca_ignora_maiusculas_e_procura_no_municipio()
    {
        Inserir(Fabrica.Cafe("Clássico", municipio: "Iúna"), Fabrica.Cafe("Doce", municipio: "Irupi"));

        var pagina = await CriarServico().ListarAsync("IRUPI", 1, 10);

        Assert.Equal("Doce", Assert.Single(pagina.Itens).Nome);
    }

    [Fact]
    public async Task Busca_sem_resultado_devolve_lista_vazia_e_total_zero()
    {
        Inserir(Fabrica.Cafe());

        var pagina = await CriarServico().ListarAsync("zzzz", 1, 10);

        Assert.Empty(pagina.Itens);
        Assert.Equal(0, pagina.Total);
        Assert.Equal(0, pagina.TotalPaginas);
    }

    [Fact]
    public async Task Obter_inexistente_devolve_nao_encontrado()
    {
        var resultado = await CriarServico().ObterAsync(9999);
        Assert.Equal(TipoErro.NaoEncontrado, resultado.Erro);
    }

    [Fact]
    public async Task Criar_guarda_data_da_torra_em_utc()
    {
        var torra = new DateTimeOffset(2026, 10, 20, 9, 0, 0, TimeSpan.FromHours(-3));

        var resultado = await CriarServico().CriarAsync(Fabrica.CafeDto(torra));

        Assert.True(resultado.Sucesso);
        using var db = _banco.CriarContexto();
        var salvo = db.Cafes.Single();
        Assert.Equal(new DateTime(2026, 10, 20, 12, 0, 0), salvo.DataTorra); // 09h em -03:00 = 12h UTC
    }

    [Fact]
    public async Task Criar_com_data_da_torra_no_futuro_devolve_validacao()
    {
        var resultado = await CriarServico().CriarAsync(Fabrica.CafeDto(DateTimeOffset.UtcNow.AddDays(10)));
        Assert.Equal(TipoErro.Validacao, resultado.Erro);
    }

    [Fact]
    public async Task Excluir_cafe_com_pedidos_devolve_conflito()
    {
        using (var db = _banco.CriarContexto())
        {
            var (a, _, _, cafe) = Fabrica.Popular(db);
            db.Pedidos.Add(new Pedido(cafe.Id, a.Id, 1, cafe.Preco, "Rua A, 123 — Iúna", Fabrica.Agora));
            db.SaveChanges();
        }

        var resultado = await CriarServico().ExcluirAsync(1);

        Assert.Equal(TipoErro.Conflito, resultado.Erro);
    }

    [Fact]
    public async Task Excluir_cafe_sem_pedidos_remove()
    {
        Inserir(Fabrica.Cafe());

        var resultado = await CriarServico().ExcluirAsync(1);

        Assert.True(resultado.Sucesso);
        using var db = _banco.CriarContexto();
        Assert.Empty(db.Cafes);
    }

    public void Dispose() => _banco.Dispose();
}
