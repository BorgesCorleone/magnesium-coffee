using MagnesiumCoffee.Api.Dominio;
using Microsoft.EntityFrameworkCore;

namespace MagnesiumCoffee.Api.Dados;

// Dados inventados (empresa fictícia). Idempotente: só insere o que ainda não existe.
public static class Seed
{
    public const string EmailAdmin = "admin@magnesiumcoffee.com";
    public const string SenhaClientes = "Cliente@2026";

    public static async Task ExecutarAsync(AppDbContext db, IConfiguration config, ILogger logger)
    {
        var agora = DateTime.UtcNow;

        if (!await db.Usuarios.AnyAsync(u => u.Email == EmailAdmin))
        {
            var senhaAdmin = config["Seed:AdminSenha"];
            if (string.IsNullOrWhiteSpace(senhaAdmin))
                throw new InvalidOperationException(
                    "Seed:AdminSenha não configurada. Defina a variável de ambiente Seed__AdminSenha.");

            db.Usuarios.Add(new Usuario("Administrador Magnesium", EmailAdmin,
                BCrypt.Net.BCrypt.HashPassword(senhaAdmin), Papeis.Admin, agora));
            logger.LogInformation("Seed: admin {Email} criado", EmailAdmin);
        }

        (string Nome, string Email)[] clientes =
        [
            ("Ana Lúcia Ferreira", "ana@exemplo.com"),
            ("Bruno Tavares", "bruno@exemplo.com"),
        ];
        foreach (var (nome, email) in clientes)
        {
            if (await db.Usuarios.AnyAsync(u => u.Email == email)) continue;
            db.Usuarios.Add(new Usuario(nome, email,
                BCrypt.Net.BCrypt.HashPassword(SenhaClientes), Papeis.Cliente, agora));
            logger.LogInformation("Seed: cliente {Email} criado", email);
        }

        if (!await db.Cafes.AnyAsync())
        {
            var cafes = CafesIniciais(agora);
            db.Cafes.AddRange(cafes);
            logger.LogInformation("Seed: {Quantidade} cafés inseridos", cafes.Count);
        }

        await db.SaveChangesAsync();
    }

    private static DateTime Torra(int mes, int dia) => new(2026, mes, dia, 12, 0, 0, DateTimeKind.Utc);

    private static List<Cafe> CafesIniciais(DateTime agora) =>
    [
        new("Pico da Bandeira Natural", "Sítio Alto da Serra — Família Lacerda", "Alto Caparaó",
            "Catuaí Vermelho", "Natural", "media", 1250, 86.5m, 250, 54.90m, 40, Torra(10, 2),
            "Notas de chocolate ao leite, caramelo e frutas amarelas. Corpo aveludado.", agora),
        new("Bourbon Amarelo Espera Feliz", "Fazenda Boa Esperança — José Carlos Moreira", "Espera Feliz",
            "Bourbon Amarelo", "Cereja descascado", "clara", 1100, 87.0m, 250, 62.00m, 25, Torra(10, 9),
            "Doce e floral, com acidez cítrica delicada e final de mel.", agora),
        new("Arara Fermentado Dores do Rio Preto", "Sítio Recanto Verde — Irmãs Gomes", "Dores do Rio Preto",
            "Arara", "Fermentado anaeróbico", "clara", 1300, 88.5m, 250, 79.90m, 12, Torra(10, 14),
            "Frutas vermelhas, vinho e especiarias. Microlote de 30 sacas.", agora),
        new("Irupi Clássico", "Cooperativa dos Produtores de Irupi", "Irupi",
            "Catuaí Amarelo", "Natural", "media", 950, 83.0m, 500, 49.90m, 80, Torra(9, 28),
            "Café equilibrado para o dia a dia, notas de castanhas e rapadura.", agora),
        new("Iúna Torra Escura", "Fazenda Santa Rita — Antônio Pereira", "Iúna",
            "Catuaí Vermelho", "Natural", "escura", 900, 82.0m, 500, 44.50m, 60, Torra(10, 20),
            "Encorpado, amargor agradável e notas de cacau. Ideal para espresso.", agora),
        new("Divino Mel", "Sítio Cachoeira Alegre — Família Barbosa", "Divino de São Lourenço",
            "Catucaí 2SL", "Honey", "media", 1150, 85.5m, 250, 58.00m, 30, Torra(10, 5),
            "Processo honey: dulçor de mel, laranja e baunilha.", agora),
        new("Guaçuí Gourmet", "Fazenda Vista Alegre — Marta Souza", "Guaçuí",
            "Mundo Novo", "Cereja descascado", "media", 850, 84.0m, 250, 46.90m, 50, Torra(9, 30),
            "Notas de chocolate amargo e amêndoas, acidez baixa.", agora),
        new("Ibitirama Geisha", "Sítio Pedra Menina — Lucas e Renata Vieira", "Ibitirama",
            "Geisha", "Lavado", "clara", 1400, 89.5m, 250, 129.00m, 6, Torra(10, 18),
            "Jasmim, bergamota e chá preto. Lote raríssimo de altitude.", agora),
        new("Caparaó Blend da Casa", "Magnesium Coffee — seleção de produtores", "Alto Caparaó",
            "Blend Catuaí e Bourbon", "Natural", "media", 1100, 84.5m, 1000, 139.90m, 20, Torra(10, 12),
            "Blend da casa em pacote de 1 kg, para cafeterias e escritórios.", agora),
        new("Pedra Menina Descafeinado", "Sítio Pedra Menina — Lucas e Renata Vieira", "Dores do Rio Preto",
            "Catuaí Vermelho", "Descafeinado (água)", "media", 1200, 83.5m, 250, 64.90m, 15, Torra(10, 1),
            "Descafeinado por processo com água, mantém notas de caramelo.", agora),
    ];
}
