using MagnesiumCoffee.Api.Dados;
using MagnesiumCoffee.Api.Dominio;
using MagnesiumCoffee.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace MagnesiumCoffee.Api.Servicos;

public class PedidoServico(AppDbContext db, ILogger<PedidoServico> logger)
{
    // Regra do 409: não vender mais pacotes do que há em estoque.
    public async Task<Resultado<PedidoSaidaDto>> CriarAsync(PedidoEntradaDto dto, int usuarioId)
    {
        await using var transacao = await db.Database.BeginTransactionAsync();

        var cafe = await db.Cafes.AsNoTracking().SingleOrDefaultAsync(c => c.Id == dto.CafeId);
        if (cafe is null)
            return Resultado<PedidoSaidaDto>.Falha(TipoErro.NaoEncontrado, $"Café {dto.CafeId} não encontrado.");

        // Baixa atômica: o UPDATE só afeta a linha se ainda houver estoque. Se dois pedidos chegarem
        // ao mesmo tempo, o banco garante que o segundo não deixe o estoque negativo.
        var linhas = !cafe.TemEstoque(dto.Quantidade) ? 0 : await db.Cafes
            .Where(c => c.Id == dto.CafeId && c.Estoque >= dto.Quantidade)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Estoque, c => c.Estoque - dto.Quantidade));

        if (linhas == 0)
        {
            await transacao.RollbackAsync();
            var disponivel = await db.Cafes.Where(c => c.Id == dto.CafeId).Select(c => c.Estoque).SingleAsync();
            logger.LogWarning("Pedido recusado por estoque: café {CafeId}, pedido {Quantidade}, disponível {Disponivel}, usuário {UsuarioId}",
                dto.CafeId, dto.Quantidade, disponivel, usuarioId);
            return Resultado<PedidoSaidaDto>.Falha(TipoErro.Conflito,
                $"Estoque insuficiente para \"{cafe.Nome}\": pedido de {dto.Quantidade} pacote(s), disponível {disponivel}.");
        }

        var pedido = new Pedido(cafe.Id, usuarioId, dto.Quantidade, cafe.Preco, dto.EnderecoEntrega, DateTime.UtcNow);
        db.Pedidos.Add(pedido);
        await db.SaveChangesAsync();
        await transacao.CommitAsync();

        logger.LogInformation("Pedido {PedidoId} criado: usuário {UsuarioId}, café {CafeId}, {Quantidade} un., total {Total}",
            pedido.Id, usuarioId, cafe.Id, pedido.Quantidade, pedido.Total);
        return await ObterAsync(pedido.Id, usuarioId, ehAdmin: false);
    }

    public async Task<PaginaDto<PedidoSaidaDto>> ListarAsync(int usuarioId, bool ehAdmin, string? status, int pagina, int tamanho)
    {
        (pagina, tamanho) = Paginacao.Normalizar(pagina, tamanho);
        var consulta = db.Pedidos.AsNoTracking();

        // Cliente só enxerga os próprios pedidos; admin vê todos.
        if (!ehAdmin)
            consulta = consulta.Where(p => p.UsuarioId == usuarioId);
        if (!string.IsNullOrWhiteSpace(status))
            consulta = consulta.Where(p => p.Status == status.Trim().ToLower());

        var total = await consulta.CountAsync();
        var itens = await Projetar(consulta
                .OrderByDescending(p => p.CriadoEm)
                .ThenByDescending(p => p.Id)
                .Skip((pagina - 1) * tamanho)
                .Take(tamanho))
            .ToListAsync();

        return PaginaDto<PedidoSaidaDto>.Criar(itens.Select(Finalizar).ToList(), pagina, tamanho, total);
    }

    public async Task<Resultado<PedidoSaidaDto>> ObterAsync(int id, int usuarioId, bool ehAdmin)
    {
        var dono = await db.Pedidos.Where(p => p.Id == id).Select(p => (int?)p.UsuarioId).SingleOrDefaultAsync();
        if (dono is null)
            return Resultado<PedidoSaidaDto>.Falha(TipoErro.NaoEncontrado, $"Pedido {id} não encontrado.");
        if (dono != usuarioId && !ehAdmin)
            return Resultado<PedidoSaidaDto>.Falha(TipoErro.Proibido, "Este pedido pertence a outro cliente.");

        var pedido = await Projetar(db.Pedidos.AsNoTracking().Where(p => p.Id == id)).SingleAsync();
        return Resultado<PedidoSaidaDto>.Ok(Finalizar(pedido));
    }

    public async Task<Resultado<PedidoSaidaDto>> CancelarAsync(int id, int usuarioId, bool ehAdmin)
    {
        await using var transacao = await db.Database.BeginTransactionAsync();

        var pedido = await db.Pedidos.FindAsync(id);
        if (pedido is null)
            return Resultado<PedidoSaidaDto>.Falha(TipoErro.NaoEncontrado, $"Pedido {id} não encontrado.");

        // Dono ou admin; qualquer outro cliente recebe 403.
        if (!pedido.PertenceA(usuarioId) && !ehAdmin)
        {
            logger.LogWarning("Usuário {UsuarioId} tentou cancelar o pedido {PedidoId} de outro cliente", usuarioId, id);
            return Resultado<PedidoSaidaDto>.Falha(TipoErro.Proibido, "Você só pode cancelar os seus próprios pedidos.");
        }
        if (pedido.Status == Pedido.Cancelado)
            return Resultado<PedidoSaidaDto>.Falha(TipoErro.Conflito, $"O pedido {id} já está cancelado.");

        pedido.Cancelar();
        await db.SaveChangesAsync();
        await db.Cafes.Where(c => c.Id == pedido.CafeId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Estoque, c => c.Estoque + pedido.Quantidade));
        await transacao.CommitAsync();

        logger.LogInformation("Pedido {PedidoId} cancelado por {UsuarioId}; {Quantidade} un. devolvidas ao café {CafeId}",
            id, usuarioId, pedido.Quantidade, pedido.CafeId);
        return await ObterAsync(id, usuarioId, ehAdmin);
    }

    private record PedidoLinha(int Id, int CafeId, string CafeNome, int UsuarioId, string ClienteNome,
        int Quantidade, decimal PrecoUnitario, decimal Total, string EnderecoEntrega, string Status, DateTime CriadoEm);

    private static IQueryable<PedidoLinha> Projetar(IQueryable<Pedido> consulta) =>
        consulta.Select(p => new PedidoLinha(p.Id, p.CafeId, p.Cafe!.Nome, p.UsuarioId, p.Usuario!.Nome,
            p.Quantidade, p.PrecoUnitario, p.Total, p.EnderecoEntrega, p.Status, p.CriadoEm));

    private static PedidoSaidaDto Finalizar(PedidoLinha p) => new(p.Id, p.CafeId, p.CafeNome, p.UsuarioId,
        p.ClienteNome, p.Quantidade, p.PrecoUnitario, p.Total, p.EnderecoEntrega, p.Status, Datas.ComoUtc(p.CriadoEm));
}
