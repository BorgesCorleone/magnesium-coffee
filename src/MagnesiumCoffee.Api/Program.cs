using System.Security.Claims;
using System.Text;
using MagnesiumCoffee.Api.Dados;
using MagnesiumCoffee.Api.Servicos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// --- Banco (Neon / PostgreSQL) ---
var connectionString = ConexaoBanco.Normalizar(builder.Configuration.GetConnectionString("Padrao"));
builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));

// --- JWT ---
var jwtSegredo = builder.Configuration["Jwt:Segredo"];
if (string.IsNullOrWhiteSpace(jwtSegredo) || Encoding.UTF8.GetByteCount(jwtSegredo) < 32)
    throw new InvalidOperationException(
        "Jwt:Segredo ausente ou curto demais (mínimo 32 bytes). Defina a variável de ambiente Jwt__Segredo.");
var jwtEmissor = builder.Configuration["Jwt:Emissor"] ?? "MagnesiumCoffee";
var jwtPublico = builder.Configuration["Jwt:Publico"] ?? "MagnesiumCoffee";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtEmissor,
            ValidateAudience = true,
            ValidAudience = jwtPublico,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSegredo)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role,
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddSingleton(new JwtConfig(jwtSegredo, jwtEmissor, jwtPublico,
    builder.Configuration.GetValue("Jwt:ExpiracaoHoras", 8)));
builder.Services.AddSingleton<TokenServico>();

// --- Serviços de negócio ---
builder.Services.AddScoped<AuthServico>();
builder.Services.AddScoped<CafeServico>();
builder.Services.AddScoped<PedidoServico>();

// --- CORS (front React) ---
var origens = (builder.Configuration["Cors:Origens"] ?? "http://localhost:5173")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(o => o.AddPolicy("Front", p => p
    .WithOrigins(origens)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithExposedHeaders("Location")));

// --- API, erros, saúde ---
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

// --- Swagger (ligado também em produção) ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Magnesium Coffee API",
        Version = "v1",
        Description = "Projeto escolar — empresa fictícia. E-commerce de cafés especiais dos produtores do Caparaó.",
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Cole apenas o token (sem o prefixo \"Bearer \").",
    });
    c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", doc), new List<string>() }
    });
    // Os comentários /// dos controllers viram descrições no Swagger.
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "MagnesiumCoffee.Api.xml"));
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseSwagger();
app.UseSwaggerUI(c => c.DocumentTitle = "Magnesium Coffee API");

app.UseCors("Front");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

// /health como endpoint normal (e não MapHealthChecks) para aparecer no Swagger.
app.MapGet("/health", async (HealthCheckService saude) =>
{
    var relatorio = await saude.CheckHealthAsync();
    var corpo = new
    {
        status = relatorio.Status.ToString(),
        verificacoes = relatorio.Entries.ToDictionary(e => e.Key, e => e.Value.Status.ToString()),
        duracaoMs = (int)relatorio.TotalDuration.TotalMilliseconds,
    };
    return relatorio.Status == HealthStatus.Healthy
        ? Results.Ok(corpo)
        : Results.Json(corpo, statusCode: StatusCodes.Status503ServiceUnavailable);
}).WithTags("Sistema").Produces(StatusCodes.Status200OK).Produces(StatusCodes.Status503ServiceUnavailable);

app.MapGet("/api/versao", (IHostEnvironment env) => Results.Ok(new
{
    commit = Environment.GetEnvironmentVariable("RENDER_GIT_COMMIT") ?? "local",
    ambiente = env.EnvironmentName,
})).WithTags("Sistema");

// --- Migrations + seed ao subir ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Seed");
    await db.Database.MigrateAsync();
    await Seed.ExecutarAsync(db, app.Configuration, logger);
}

app.Run();

public partial class Program { }
