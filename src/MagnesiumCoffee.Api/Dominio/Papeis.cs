namespace MagnesiumCoffee.Api.Dominio;

public static class Papeis
{
    public const string Admin = "admin";
    public const string Cliente = "cliente";

    public static bool EhValido(string papel) => papel is Admin or Cliente;
}
