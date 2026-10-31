# 02 — Autenticação (registro, login, JWT e papéis)

**Objetivo:** permitir criar conta e fazer login, devolvendo um JWT com o id e o papel do usuário,
que as próximas fatias usam para proteger rotas (401/403).

## O que foi feito
| Item | Arquivo |
|---|---|
| Padrão de retorno dos serviços | `Servicos/Resultado.cs` (`Resultado<T>` + `TipoErro`) |
| Geração do token | `Servicos/TokenServico.cs` (`JwtConfig`, `TokenServico`) |
| Regras de registro/login | `Servicos/AuthServico.cs` |
| DTOs com validação | `Dtos/AuthDtos.cs` |
| Base dos controllers | `Controllers/ApiBaseController.cs` |
| Endpoints | `Controllers/AuthController.cs` |
| Registro dos serviços e /health no Swagger | `Program.cs` |
| Banco de teste | `tests/.../BancoDeTeste.cs` |
| Testes | `tests/.../AuthServicoTests.cs` (4 testes) |

## Endpoints
| Verbo | Rota | Quem pode | Respostas |
|---|---|---|---|
| POST | `/api/auth/registro` | todos | 201, 400, 409 |
| POST | `/api/auth/login` | todos | 200, 400, 401 |
| GET | `/api/auth/eu` | logado | 200, 401 |
| GET | `/health` | todos | 200, 503 |

## Decisões e por quê

### Resultado<T> em vez de exceção (fato técnico 6)
O serviço não sabe nada de HTTP: devolve `Resultado<T>.Ok(valor)` ou
`Resultado<T>.Falha(TipoErro.Conflito, "mensagem")`. O `ApiBaseController.Erro(...)` traduz:

| TipoErro | HTTP |
|---|---|
| Validacao | 400 |
| NaoAutenticado | 401 |
| Proibido | 403 |
| NaoEncontrado | 404 |
| Conflito | 409 |

Sempre com `Problem(...)`, ou seja, corpo em ProblemDetails com a mensagem no `detail`.
**Diferença do padrão das aulas:** acrescentei `NaoAutenticado` (401) aos quatro tipos do
enunciado, para o login com senha errada não virar 400.

**Por quê:** exceção é cara e esconde o fluxo; erro de negócio é um resultado esperado, não algo
excepcional. Exceção fica para o inesperado (cai no `UseExceptionHandler` → 500).

### Registro
- E-mail normalizado (`Trim` + minúsculas): `Carla@Exemplo.com` e `carla@exemplo.com` são a mesma conta.
- E-mail repetido → **409 Conflito** (além do índice único no banco).
- **Sempre cria `cliente`.** O DTO nem tem campo de papel, então ninguém se registra como admin;
  admin só nasce no seed (fato técnico 7).
- Senha guardada com `BCrypt.HashPassword` (hash com salt; não dá para "desfazer").
- Devolve **201** com o token, para o cliente já sair logado.

### Login
- Usuário inexistente e senha errada dão **a mesma mensagem** ("E-mail ou senha inválidos"),
  para não revelar quais e-mails estão cadastrados.
- Sucesso → token + dados do usuário.

### Token (JWT)
- Claims: `NameIdentifier` (id), `Name`, `Email`, `Role` (`admin`/`cliente`).
- Assinado com HMAC-SHA256 usando `Jwt__Segredo`; emissor/público `MagnesiumCoffee`; expira em 8 h
  (`Jwt:ExpiracaoHoras`).
- Gerado com `JsonWebTokenHandler` (biblioteca nova do .NET, já vem com o pacote JwtBearer).
- No `Program.cs`, `RoleClaimType = ClaimTypes.Role` faz o `[Authorize(Roles = "admin")]` ler o papel do token.

### Identidade sempre pelos claims (fato técnico 7)
`ApiBaseController.UsuarioId` lê o id de `ClaimTypes.NameIdentifier` do token. Nenhum endpoint
aceita "id do usuário" no corpo: quem você é vem do token assinado, não do que você diz.
`GET /api/auth/eu` demonstra isso.

### Validação automática (400)
`[ApiController]` + DataAnnotations nos DTOs (`[Required]`, `[EmailAddress]`, `[StringLength]`):
corpo inválido nem chega ao controller e volta **400** com a lista de erros por campo
(`ValidationProblemDetails`).

### Logs estruturados
`ILogger` com templates, não interpolação: `"Usuário {UsuarioId} registrado como {Papel}"`.
Os campos viram propriedades pesquisáveis no log da Render. Senha nunca é logada.

### /health no Swagger (correção)
No esqueleto, `/health` usava `app.MapHealthChecks(...)`, que **funciona mas não aparece no
Swagger** (o Swashbuckle só documenta endpoints do ApiExplorer). Trocado por um `MapGet("/health")`
que chama o `HealthCheckService` (o mesmo `AddDbContextCheck`) e devolve JSON:
`{ "status": "Healthy", "verificacoes": { "AppDbContext": "Healthy" }, "duracaoMs": 12 }`,
ou **503** se o banco estiver fora. A Render continua usando `/health` como health check.

> **Erro da IA para o seminário:** a primeira versão gerada usava `MapHealthChecks`, e o `/health`
> não aparecia no Swagger publicado. Percebi testando o Swagger na Render; a correção está no
> `Program.cs` (bloco `app.MapGet("/health", ...)`).

## Testes (SQLite em memória — fato técnico 13)
`BancoDeTeste` abre uma `SqliteConnection("DataSource=:memory:")`, mantém aberta e roda
`EnsureCreated()`: cada teste tem um banco novo e vazio.
1. Registro cria **cliente** e guarda a senha em **hash** BCrypt.
2. E-mail repetido (com maiúsculas diferentes) → **Conflito**.
3. Senha errada → **NaoAutenticado**.
4. Login correto → token com **id e papel** nos claims.

## Como demonstrar no Swagger
1. `POST /api/auth/login` com `{ "email": "ana@exemplo.com", "senha": "Cliente@2026" }` → 200 e token.
2. Botão **Authorize** → colar só o token → `GET /api/auth/eu` → 200 com os dados da Ana.
3. Sem Authorize, `GET /api/auth/eu` → **401** em ProblemDetails.
4. `POST /api/auth/registro` com e-mail `ana@exemplo.com` → **409**.
5. `POST /api/auth/registro` com `{ "nome": "", "email": "x", "senha": "1" }` → **400** com erros por campo.

## Perguntas prováveis
- *Como a API sabe quem é o usuário?* Pelo claim `NameIdentifier` do JWT, validado pela assinatura.
- *E se alguém alterar o papel dentro do token?* A assinatura deixa de bater e o token é rejeitado (401).
- *Por que BCrypt e não SHA-256?* BCrypt é lento de propósito e tem salt: dificulta força bruta.
- *Por que o login não diz se o e-mail existe?* Para não permitir descobrir contas cadastradas.
- *Por que o registro não aceita papel?* Para ninguém virar admin sozinho; admin só via seed.
