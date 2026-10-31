using Npgsql;

namespace MagnesiumCoffee.Api.Dados;

// A Neon entrega a URI (postgresql://usuario:senha@host/banco?sslmode=require&channel_binding=require).
// O Npgsql só entende o formato chave=valor, então a URI é convertida aqui.
public static class ConexaoBanco
{
    public static string Normalizar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new InvalidOperationException(
                "ConnectionStrings:Padrao está vazia. Defina a variável de ambiente ConnectionStrings__Padrao " +
                "com a connection string da Neon (postgresql://...) ou no formato Npgsql (Host=...;Database=...).");

        valor = valor.Trim();
        var ehUri = valor.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
                    || valor.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase);
        if (!ehUri)
            return valor;

        var uri = new Uri(valor);
        var credenciais = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(credenciais[0]),
            Password = credenciais.Length > 1 ? Uri.UnescapeDataString(credenciais[1]) : null,
            SslMode = SslMode.VerifyFull,
            ChannelBinding = ChannelBinding.Require,
        };
        return builder.ConnectionString;
    }
}
