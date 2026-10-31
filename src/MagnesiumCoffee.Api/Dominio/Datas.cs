namespace MagnesiumCoffee.Api.Dominio;

// O Npgsql só aceita DateTime com Kind=Utc em "timestamp with time zone".
internal static class Datas
{
    public static DateTime ExigirUtc(DateTime data, string campo)
    {
        if (data.Kind != DateTimeKind.Utc)
            throw new ArgumentException($"{campo} precisa estar em UTC (Kind=Utc).", campo);
        return data;
    }
}
