using MagnesiumCoffee.Api.Dados;
using Npgsql;

namespace MagnesiumCoffee.Tests;

public class ConexaoBancoTests
{
    [Fact]
    public void Converte_uri_da_neon_para_formato_npgsql()
    {
        var uri = "postgresql://neondb_owner:s3nh%40x@ep-teste-123.us-east-1.aws.neon.tech/neondb?sslmode=require&channel_binding=require";

        var cs = new NpgsqlConnectionStringBuilder(ConexaoBanco.Normalizar(uri));

        Assert.Equal("ep-teste-123.us-east-1.aws.neon.tech", cs.Host);
        Assert.Equal(5432, cs.Port);
        Assert.Equal("neondb", cs.Database);
        Assert.Equal("neondb_owner", cs.Username);
        Assert.Equal("s3nh@x", cs.Password);
        Assert.Equal(SslMode.VerifyFull, cs.SslMode);
        Assert.Equal(ChannelBinding.Require, cs.ChannelBinding);
    }

    [Fact]
    public void Mantem_string_que_ja_esta_no_formato_npgsql()
    {
        var original = "Host=localhost;Database=cafe;Username=u;Password=p";
        Assert.Equal(original, ConexaoBanco.Normalizar(original));
    }

    [Fact]
    public void String_vazia_lanca_erro_claro()
    {
        var erro = Assert.Throws<InvalidOperationException>(() => ConexaoBanco.Normalizar(""));
        Assert.Contains("ConnectionStrings__Padrao", erro.Message);
    }
}
