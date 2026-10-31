using System.Security.Claims;
using MagnesiumCoffee.Api.Dominio;
using MagnesiumCoffee.Api.Dtos;
using MagnesiumCoffee.Api.Servicos;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;

namespace MagnesiumCoffee.Tests;

public class AuthServicoTests : IDisposable
{
    private readonly BancoDeTeste _banco = new();
    private readonly TokenServico _tokens =
        new(new JwtConfig("segredo-de-teste-com-mais-de-32-bytes-123456", "teste", "teste", 1));

    private AuthServico CriarServico() =>
        new(_banco.CriarContexto(), _tokens, NullLogger<AuthServico>.Instance);

    [Fact]
    public async Task Registro_sempre_cria_cliente_com_senha_em_hash()
    {
        var resultado = await CriarServico().RegistrarAsync(new RegistroDto("Carla", "Carla@Exemplo.com", "SenhaForte1"));

        Assert.True(resultado.Sucesso);
        Assert.Equal(Papeis.Cliente, resultado.Valor!.Usuario.Papel);
        Assert.Equal("carla@exemplo.com", resultado.Valor.Usuario.Email);

        using var db = _banco.CriarContexto();
        var salvo = db.Usuarios.Single();
        Assert.NotEqual("SenhaForte1", salvo.SenhaHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("SenhaForte1", salvo.SenhaHash));
    }

    [Fact]
    public async Task Registro_com_email_repetido_devolve_conflito()
    {
        await CriarServico().RegistrarAsync(new RegistroDto("Carla", "carla@exemplo.com", "SenhaForte1"));

        var resultado = await CriarServico().RegistrarAsync(new RegistroDto("Outra", "CARLA@exemplo.com", "OutraSenha1"));

        Assert.False(resultado.Sucesso);
        Assert.Equal(TipoErro.Conflito, resultado.Erro);
    }

    [Fact]
    public async Task Login_com_senha_errada_devolve_nao_autenticado()
    {
        await CriarServico().RegistrarAsync(new RegistroDto("Carla", "carla@exemplo.com", "SenhaForte1"));

        var resultado = await CriarServico().LoginAsync(new LoginDto("carla@exemplo.com", "errada123"));

        Assert.False(resultado.Sucesso);
        Assert.Equal(TipoErro.NaoAutenticado, resultado.Erro);
    }

    [Fact]
    public async Task Login_correto_gera_token_com_id_e_papel_nos_claims()
    {
        var registro = await CriarServico().RegistrarAsync(new RegistroDto("Carla", "carla@exemplo.com", "SenhaForte1"));

        var resultado = await CriarServico().LoginAsync(new LoginDto("carla@exemplo.com", "SenhaForte1"));

        Assert.True(resultado.Sucesso);
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(resultado.Valor!.Token);
        Assert.Equal(registro.Valor!.Usuario.Id.ToString(), jwt.GetClaim(ClaimTypes.NameIdentifier).Value);
        Assert.Equal(Papeis.Cliente, jwt.GetClaim(ClaimTypes.Role).Value);
    }

    public void Dispose() => _banco.Dispose();
}
