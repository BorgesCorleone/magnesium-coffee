using MagnesiumCoffee.Api.Dados;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MagnesiumCoffee.Tests;

// Um banco SQLite em memória novo por teste. A conexão precisa ficar aberta: fechou, o banco some.
public sealed class BancoDeTeste : IDisposable
{
    private readonly SqliteConnection _conexao;

    public BancoDeTeste()
    {
        _conexao = new SqliteConnection("DataSource=:memory:");
        _conexao.Open();
        using var db = CriarContexto();
        db.Database.EnsureCreated();
    }

    public AppDbContext CriarContexto() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conexao).Options);

    public void Dispose() => _conexao.Dispose();
}
