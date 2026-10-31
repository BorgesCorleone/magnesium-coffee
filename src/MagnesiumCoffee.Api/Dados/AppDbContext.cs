using MagnesiumCoffee.Api.Dominio;
using Microsoft.EntityFrameworkCore;

namespace MagnesiumCoffee.Api.Dados;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Cafe> Cafes => Set<Cafe>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(e =>
        {
            e.Property(u => u.Nome).HasMaxLength(100).IsRequired();
            e.Property(u => u.Email).HasMaxLength(150).IsRequired();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.SenhaHash).HasMaxLength(100).IsRequired();
            e.Property(u => u.Papel).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<Cafe>(e =>
        {
            e.Property(c => c.Nome).HasMaxLength(100).IsRequired();
            e.Property(c => c.Produtor).HasMaxLength(100).IsRequired();
            e.Property(c => c.Municipio).HasMaxLength(60).IsRequired();
            e.Property(c => c.Variedade).HasMaxLength(60).IsRequired();
            e.Property(c => c.Processo).HasMaxLength(60).IsRequired();
            e.Property(c => c.Torra).HasMaxLength(10).IsRequired();
            e.Property(c => c.PontuacaoSca).HasPrecision(4, 1);
            e.Property(c => c.Preco).HasPrecision(10, 2);
            e.Property(c => c.Descricao).HasMaxLength(500);
            e.HasIndex(c => c.Nome);
        });

        modelBuilder.Entity<Pedido>(e =>
        {
            e.Property(p => p.PrecoUnitario).HasPrecision(10, 2);
            e.Property(p => p.Total).HasPrecision(12, 2);
            e.Property(p => p.EnderecoEntrega).HasMaxLength(200).IsRequired();
            e.Property(p => p.Status).HasMaxLength(20).IsRequired();

            // 1 Café → N Pedidos. Restrict: não apaga café que já foi vendido.
            e.HasOne(p => p.Cafe).WithMany(c => c.Pedidos)
                .HasForeignKey(p => p.CafeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Usuario).WithMany(u => u.Pedidos)
                .HasForeignKey(p => p.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
