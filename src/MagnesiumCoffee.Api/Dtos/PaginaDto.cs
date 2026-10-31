namespace MagnesiumCoffee.Api.Dtos;

public record PaginaDto<T>(IReadOnlyList<T> Itens, int Pagina, int Tamanho, int Total, int TotalPaginas)
{
    public static PaginaDto<T> Criar(IReadOnlyList<T> itens, int pagina, int tamanho, int total) =>
        new(itens, pagina, tamanho, total, total == 0 ? 0 : (int)Math.Ceiling(total / (double)tamanho));
}

public static class Paginacao
{
    public const int TamanhoMaximo = 50;

    public static (int Pagina, int Tamanho) Normalizar(int pagina, int tamanho) =>
        (Math.Max(1, pagina), Math.Clamp(tamanho, 1, TamanhoMaximo));
}
