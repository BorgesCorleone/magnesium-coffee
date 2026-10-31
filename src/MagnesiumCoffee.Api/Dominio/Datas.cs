namespace MagnesiumCoffee.Api.Dominio;

// O Npgsql só aceita DateTime com Kind=Utc em "timestamp with time zone".
public static class Datas
{
    public static DateTime ExigirUtc(DateTime data, string campo)
    {
        if (data.Kind != DateTimeKind.Utc)
            throw new ArgumentException($"{campo} precisa estar em UTC (Kind=Utc).", campo);
        return data;
    }

    // Na leitura, o SQLite dos testes devolve Kind=Unspecified; o valor gravado é sempre UTC.
    public static DateTimeOffset ComoUtc(DateTime data) =>
        new(DateTime.SpecifyKind(data, DateTimeKind.Utc));
}
