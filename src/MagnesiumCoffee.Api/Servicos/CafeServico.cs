using MagnesiumCoffee.Api.Dados;
using MagnesiumCoffee.Api.Dominio;
using MagnesiumCoffee.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace MagnesiumCoffee.Api.Servicos;

public class CafeServico(AppDbContext db, ILogger<CafeServico> logger)
{
    public async Task<PaginaDto<CafeSaidaDto>> ListarAsync(string? busca, int pagina, int tamanho)
    {
        (pagina, tamanho) = Paginacao.Normalizar(pagina, tamanho);
        var consulta = db.Cafes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            // ToLower().Contains() roda no Postgres e no SQLite dos testes (ILike é só Postgres).
            var termo = busca.Trim().ToLower();
            consulta = consulta.Where(c =>
                c.Nome.ToLower().Contains(termo) ||
                c.Produtor.ToLower().Contains(termo) ||
                c.Municipio.ToLower().Contains(termo) ||
                c.Variedade.ToLower().Contains(termo) ||
                c.Processo.ToLower().Contains(termo));
        }

        var total = await consulta.CountAsync();
        var cafes = await consulta
            .OrderBy(c => c.Nome)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync();

        return PaginaDto<CafeSaidaDto>.Criar(cafes.Select(ParaDto).ToList(), pagina, tamanho, total);
    }

    public async Task<Resultado<CafeSaidaDto>> ObterAsync(int id)
    {
        var cafe = await db.Cafes.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id);
        return cafe is null
            ? Resultado<CafeSaidaDto>.Falha(TipoErro.NaoEncontrado, $"Café {id} não encontrado.")
            : Resultado<CafeSaidaDto>.Ok(ParaDto(cafe));
    }

    public async Task<Resultado<CafeSaidaDto>> CriarAsync(CafeEntradaDto dto)
    {
        if (ValidarDataTorra(dto) is { } erro)
            return Resultado<CafeSaidaDto>.Falha(TipoErro.Validacao, erro);

        var cafe = new Cafe(dto.Nome, dto.Produtor, dto.Municipio, dto.Variedade, dto.Processo,
            dto.Torra, dto.AltitudeMetros, dto.PontuacaoSca, dto.PesoGramas, dto.Preco, dto.Estoque,
            dto.DataTorra!.Value.UtcDateTime, dto.Descricao ?? "", DateTime.UtcNow);
        db.Cafes.Add(cafe);
        await db.SaveChangesAsync();

        logger.LogInformation("Café {CafeId} \"{Nome}\" cadastrado com estoque {Estoque}", cafe.Id, cafe.Nome, cafe.Estoque);
        return Resultado<CafeSaidaDto>.Ok(ParaDto(cafe));
    }

    public async Task<Resultado<CafeSaidaDto>> AtualizarAsync(int id, CafeEntradaDto dto)
    {
        var cafe = await db.Cafes.FindAsync(id);
        if (cafe is null)
            return Resultado<CafeSaidaDto>.Falha(TipoErro.NaoEncontrado, $"Café {id} não encontrado.");
        if (ValidarDataTorra(dto) is { } erro)
            return Resultado<CafeSaidaDto>.Falha(TipoErro.Validacao, erro);

        cafe.Atualizar(dto.Nome, dto.Produtor, dto.Municipio, dto.Variedade, dto.Processo, dto.Torra,
            dto.AltitudeMetros, dto.PontuacaoSca, dto.PesoGramas, dto.Preco, dto.Estoque,
            dto.DataTorra!.Value.UtcDateTime, dto.Descricao ?? "");
        await db.SaveChangesAsync();

        logger.LogInformation("Café {CafeId} atualizado: preço {Preco}, estoque {Estoque}", cafe.Id, cafe.Preco, cafe.Estoque);
        return Resultado<CafeSaidaDto>.Ok(ParaDto(cafe));
    }

    public async Task<Resultado<bool>> ExcluirAsync(int id)
    {
        var cafe = await db.Cafes.FindAsync(id);
        if (cafe is null)
            return Resultado<bool>.Falha(TipoErro.NaoEncontrado, $"Café {id} não encontrado.");

        if (await db.Pedidos.AnyAsync(p => p.CafeId == id))
        {
            logger.LogWarning("Exclusão do café {CafeId} recusada: possui pedidos", id);
            return Resultado<bool>.Falha(TipoErro.Conflito,
                $"O café \"{cafe.Nome}\" já tem pedidos e não pode ser excluído. Zere o estoque para tirá-lo de venda.");
        }

        db.Cafes.Remove(cafe);
        await db.SaveChangesAsync();
        logger.LogInformation("Café {CafeId} excluído", id);
        return Resultado<bool>.Ok(true);
    }

    private static string? ValidarDataTorra(CafeEntradaDto dto) =>
        dto.DataTorra > DateTimeOffset.UtcNow.AddDays(1)
            ? "A data da torra não pode estar no futuro."
            : null;

    internal static CafeSaidaDto ParaDto(Cafe c) => new(c.Id, c.Nome, c.Produtor, c.Municipio,
        c.Variedade, c.Processo, c.Torra, c.AltitudeMetros, c.PontuacaoSca, c.PesoGramas, c.Preco,
        c.Estoque, c.Estoque > 0, Datas.ComoUtc(c.DataTorra), c.Descricao);
}
