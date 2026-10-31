# 01 — Esqueleto no ar

**Objetivo:** ter a API publicada antes de qualquer regra de negócio, com banco, Swagger, erros
padronizados, health check, versão, Docker, CI e deploy automático funcionando ponta a ponta.

## O que foi feito
| Item | Arquivo |
|---|---|
| Solution com API + testes | `MagnesiumCoffee.slnx` |
| Pacotes nas versões fixas do enunciado | `src/MagnesiumCoffee.Api/MagnesiumCoffee.Api.csproj`, `tests/.../MagnesiumCoffee.Tests.csproj` |
| Entidades com regras | `Dominio/Usuario.cs`, `Dominio/Cafe.cs`, `Dominio/Pedido.cs`, `Dominio/Papeis.cs`, `Dominio/Datas.cs` |
| Mapeamento do banco | `Dados/AppDbContext.cs` |
| Primeira migration | `Dados/Migrations/*_Inicial.cs` |
| Seed idempotente | `Dados/Seed.cs` |
| Conversão da connection string da Neon | `Dados/ConexaoBanco.cs` |
| Configuração geral | `Program.cs` |
| Imagem Docker | `Dockerfile`, `.dockerignore` |
| CI | `.github/workflows/ci.yml` |
| Deploy na Render | `render.yaml` |
| Testes de domínio | `tests/.../DominioTests.cs`, `tests/.../ConexaoBancoTests.cs` |

## Decisões e por quê

### Domínio
- **Cafe** (entidade principal): nome, produtor, município do Caparaó, variedade, processo, torra
  (`clara`/`media`/`escura`), altitude, pontuação SCA, peso, preço, **estoque** e data da torra.
- **Pedido** (segunda entidade, 1 Café → N Pedidos): guarda `CafeId`, `UsuarioId` (dono),
  quantidade, **preço unitário copiado na hora da compra** (se o admin mudar o preço depois, o
  pedido antigo não muda) e total calculado.
- **Regra do 409** (implementada na fatia de pedidos): pedir quantidade maior que o estoque.
- As entidades têm `private set` e construtor que valida: um objeto inválido não chega a existir.
  O construtor vazio `private` existe só para o EF Core.
- `DeleteBehavior.Restrict` em Pedido → Cafe: não se apaga um café que já foi vendido.

### Datas em UTC (fato técnico 2)
O Npgsql só grava `DateTime` com `Kind=Utc` em colunas `timestamp with time zone`.
`Datas.ExigirUtc` faz o construtor das entidades **recusar** qualquer data que não seja UTC.
Os DTOs recebem `DateTimeOffset` e o serviço passa `.UtcDateTime` para a entidade.

### Connection string da Neon (decisão diferente do enunciado)
O enunciado pede o formato Npgsql (`Host=...;Database=...`). Optamos por colar **exatamente a
string que a Neon mostra** (`postgresql://usuario:senha@host/banco?sslmode=require...`) e converter
no código, em `ConexaoBanco.Normalizar`:
- se vier vazia → `InvalidOperationException` com mensagem clara (fato técnico 1);
- se já estiver no formato Npgsql → usa como está;
- se for URI → monta com `NpgsqlConnectionStringBuilder`, forçando `SSL Mode=VerifyFull` e
  `Channel Binding=Require`. Senha com caractere especial (`%40` = `@`) é decodificada.
- Coberto por 3 testes em `ConexaoBancoTests.cs`.

**Por quê:** evita erro de digitação ao montar a string à mão e funciona com qualquer um dos formatos.

### Program.cs
- Lê `ConnectionStrings:Padrao` e `Jwt:Segredo` da configuração; o ASP.NET Core transforma a
  variável de ambiente `ConnectionStrings__Padrao` em `ConnectionStrings:Padrao` (o `__` vira `:`).
- Segredo do JWT com menos de 32 bytes → erro na subida (HMAC-SHA256 exige chave de 256 bits).
- **Swagger ligado em produção** (fora do `if IsDevelopment`), porque a correção e o seminário são
  feitos no Swagger publicado. Botão **Authorize** do tipo `Http`/`bearer`: cola-se só o token.
- Swashbuckle 10 usa OpenAPI.NET 2.x: `using Microsoft.OpenApi;` e `OpenApiSecuritySchemeReference`
  (o padrão antigo com `Reference = new OpenApiReference` não compila mais).
- `AddProblemDetails()` + `UseExceptionHandler()` + `UseStatusCodePages()`: todo erro (inclusive
  401 e 404 sem corpo) sai no formato **ProblemDetails** (RFC 7807).
- CORS com `WithOrigins` lido de `Cors:Origens` (padrão `http://localhost:5173`), **antes** da
  autenticação. Nunca `AllowAnyOrigin`.
- `GET /` redireciona para `/swagger`.
- `GET /api/versao` devolve o commit publicado (`RENDER_GIT_COMMIT`, que a Render preenche).
- Ao subir: `MigrateAsync()` aplica migrations pendentes e o seed roda. Na **primeira** subida o EF
  loga um `fail ... __EFMigrationsHistory` (a tabela ainda não existe) e segue: é esperado.
- `public partial class Program { }` no final: deixa a classe visível para projetos de teste.

### Seed (idempotente)
Só insere o que não existe: admin (`admin@magnesiumcoffee.com`, senha da variável
`Seed__AdminSenha`), 2 clientes (`ana@exemplo.com`, `bruno@exemplo.com`, senha `Cliente@2026`) e
10 cafés inventados de municípios reais do Caparaó (Alto Caparaó, Espera Feliz, Dores do Rio Preto,
Irupi, Iúna, Divino de São Lourenço, Guaçuí, Ibitirama). Senhas sempre com BCrypt.

### Docker
Dois estágios: `sdk:10.0` compila e publica; `aspnet:10.0` (menor, sem compilador) só executa.
`ASPNETCORE_HTTP_PORTS=10000` porque é a porta que a Render espera. O `.dockerignore` tira `bin`,
`obj` e a pasta de testes da imagem.

### CI + deploy
- `ci.yml`: a cada push/PR no `main` roda `restore`, `build -c Release -warnaserror` (aviso vira
  erro) e `test`.
- `render.yaml` (Blueprint): Web Service Docker, plano Free, região Virginia (perto do banco na
  Neon, US East), health check em `/health` e `autoDeployTrigger: checksPass` — **a Render só
  publica depois que o CI passa**, então código quebrado não vai ao ar.
- Segredos fora do repositório: `ConnectionStrings__Padrao` e `Seed__AdminSenha` são informados no
  painel (`sync: false`); `Jwt__Segredo` é gerado pela própria Render (`generateValue: true`).

## Como demonstrar
- `https://magnesium-coffee-api.onrender.com/` → cai no Swagger.
- `GET /health` → `Healthy` (prova que a API conecta no Postgres da Neon).
- `GET /api/versao` → `commit` igual ao último commit do GitHub (prova do deploy automático).

## Perguntas prováveis
- *Por que o construtor da entidade valida?* Para que nenhum objeto inválido exista; a regra fica
  junto do dado e é testável sem banco.
- *Por que `DateTimeOffset` no DTO e `DateTime` UTC na entidade?* O DTO aceita qualquer fuso do
  cliente; a entidade guarda um instante único em UTC, exigido pelo Npgsql.
- *Por que dois estágios no Docker?* A imagem final não leva o SDK: fica menor e mais segura.
- *Onde estão os segredos?* Em variáveis de ambiente na Render; o `appsettings.json` commitado tem
  `"Padrao": ""`.
- *O que acontece se o CI falhar?* A Render não publica (`checksPass`); fica no ar a versão anterior.
