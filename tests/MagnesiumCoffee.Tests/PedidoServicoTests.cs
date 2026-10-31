using MagnesiumCoffee.Api.Dominio;
using MagnesiumCoffee.Api.Servicos;
using Microsoft.Extensions.Logging.Abstractions;

namespace MagnesiumCoffee.Tests;

public class PedidoServicoTests : IDisposable
{
    private readonly BancoDeTeste _banco = new();
    private readonly Usuario _a, _b, _admin;
    private readonly Cafe _cafe;

    public PedidoServicoTests()
    {
        using var db = _banco.CriarContexto();
        (_a, _b, _admin, _cafe) = Fabrica.Popular(db, estoque: 5);
    }

    private PedidoServico CriarServico() => new(_banco.CriarContexto(), NullLogger<PedidoServico>.Instance);

    private int EstoqueAtual()
    {
        using var db = _banco.CriarContexto();
        return db.Cafes.Single(c => c.Id == _cafe.Id).Estoque;
    }

    [Fact]
    public async Task Criar_baixa_estoque_e_copia_o_preco()
    {
        var resultado = await CriarServico().CriarAsync(Fabrica.PedidoDto(_cafe.Id, 2), _a.Id);

        Assert.True(resultado.Sucesso);
        Assert.Equal(_a.Id, resultado.Valor!.UsuarioId);
        Assert.Equal(100m, resultado.Valor.Total);
        Assert.Equal(3, EstoqueAtual());
    }

    [Fact]
    public async Task Quantidade_maior_que_estoque_devolve_conflito_e_nao_mexe_no_estoque()
    {
        var resultado = await CriarServico().CriarAsync(Fabrica.PedidoDto(_cafe.Id, 6), _a.Id);

        Assert.Equal(TipoErro.Conflito, resultado.Erro);
        Assert.Contains("disponível 5", resultado.Mensagem);
        Assert.Equal(5, EstoqueAtual());
        using var db = _banco.CriarContexto();
        Assert.Empty(db.Pedidos);
    }

    [Fact]
    public async Task Segundo_pedido_que_esgota_o_estoque_devolve_conflito()
    {
        await CriarServico().CriarAsync(Fabrica.PedidoDto(_cafe.Id, 4), _a.Id);

        var resultado = await CriarServico().CriarAsync(Fabrica.PedidoDto(_cafe.Id, 2), _b.Id);

        Assert.Equal(TipoErro.Conflito, resultado.Erro);
        Assert.Equal(1, EstoqueAtual());
    }

    [Fact]
    public async Task Pedido_de_cafe_inexistente_devolve_nao_encontrado()
    {
        var resultado = await CriarServico().CriarAsync(Fabrica.PedidoDto(9999, 1), _a.Id);
        Assert.Equal(TipoErro.NaoEncontrado, resultado.Erro);
    }

    [Fact]
    public async Task Cliente_lista_so_os_proprios_pedidos_e_admin_lista_todos()
    {
        await CriarServico().CriarAsync(Fabrica.PedidoDto(_cafe.Id, 1), _a.Id);
        await CriarServico().CriarAsync(Fabrica.PedidoDto(_cafe.Id, 1), _b.Id);

        var doA = await CriarServico().ListarAsync(_a.Id, ehAdmin: false, null, 1, 10);
        var doAdmin = await CriarServico().ListarAsync(_admin.Id, ehAdmin: true, null, 1, 10);

        Assert.All(doA.Itens, p => Assert.Equal(_a.Id, p.UsuarioId));
        Assert.Equal(1, doA.Total);
        Assert.Equal(2, doAdmin.Total);
    }

    [Fact]
    public async Task Cliente_nao_ve_pedido_de_outro_cliente()
    {
        var pedido = await CriarServico().CriarAsync(Fabrica.PedidoDto(_cafe.Id, 1), _a.Id);

        var resultado = await CriarServico().ObterAsync(pedido.Valor!.Id, _b.Id, ehAdmin: false);

        Assert.Equal(TipoErro.Proibido, resultado.Erro);
    }

    [Fact]
    public async Task Cliente_nao_cancela_pedido_de_outro_cliente()
    {
        var pedido = await CriarServico().CriarAsync(Fabrica.PedidoDto(_cafe.Id, 1), _a.Id);

        var resultado = await CriarServico().CancelarAsync(pedido.Valor!.Id, _b.Id, ehAdmin: false);

        Assert.Equal(TipoErro.Proibido, resultado.Erro);
        Assert.Equal(4, EstoqueAtual());
    }

    [Fact]
    public async Task Admin_cancela_pedido_de_cliente_e_estoque_volta()
    {
        var pedido = await CriarServico().CriarAsync(Fabrica.PedidoDto(_cafe.Id, 3), _a.Id);

        var resultado = await CriarServico().CancelarAsync(pedido.Valor!.Id, _admin.Id, ehAdmin: true);

        Assert.True(resultado.Sucesso);
        Assert.Equal(Pedido.Cancelado, resultado.Valor!.Status);
        Assert.Equal(5, EstoqueAtual());
    }

    [Fact]
    public async Task Cancelar_pedido_ja_cancelado_devolve_conflito()
    {
        var pedido = await CriarServico().CriarAsync(Fabrica.PedidoDto(_cafe.Id, 1), _a.Id);
        await CriarServico().CancelarAsync(pedido.Valor!.Id, _a.Id, ehAdmin: false);

        var resultado = await CriarServico().CancelarAsync(pedido.Valor.Id, _a.Id, ehAdmin: false);

        Assert.Equal(TipoErro.Conflito, resultado.Erro);
        Assert.Equal(5, EstoqueAtual());
    }

    public void Dispose() => _banco.Dispose();
}
