# Magnesium Coffee API
**Nicho:** Magnesium Coffe: e-commerce de café dos produtores do Caparaó · **Aluno:** Eduardo Borges Cordeiro (@BorgesCorleone)
**API no ar:** https://magnesium-coffee-api.onrender.com/swagger
**Saúde e versão:** https://magnesium-coffee-api.onrender.com/health · https://magnesium-coffee-api.onrender.com/api/versao
**Front-end:** https://magnesium-coffee-web.onrender.com

> Projeto escolar — empresa fictícia. Cafés, produtores e pedidos são inventados.
> A instância gratuita da Render dorme sem uso: a primeira requisição pode levar até 1 minuto.

## O que a API faz
Loja virtual de cafés especiais de altitude da Serra do Caparaó (ES/MG). O **admin** mantém o
catálogo (cafés com produtor, município, variedade, processo, torra, altitude, nota SCA, preço e
estoque). O **cliente** cria conta, navega pelo catálogo público, faz pedidos e acompanha ou cancela
os seus.

**Regra de negócio (409):** um pedido não pode pedir mais pacotes do que há em estoque. A baixa é
feita dentro de uma transação, com um `UPDATE` condicional (`... WHERE Estoque >= quantidade`), então
nem dois pedidos simultâneos conseguem deixar o estoque negativo. Exemplo de resposta:
`409 — Estoque insuficiente para "Ibitirama Geisha": pedido de 50 pacote(s), disponível 6.`
Outros 409: e-mail já cadastrado, excluir café que já tem pedidos, cancelar pedido já cancelado.

## Recursos e endpoints
| Verbo | Rota | Quem pode | Respostas |
|---|---|---|---|
| POST | /api/auth/registro | todos (cria sempre "cliente") | 201, 400, 409 |
| POST | /api/auth/login | todos | 200, 400, 401 |
| GET | /api/auth/eu | logado | 200, 401 |
| GET | /api/cafes?busca=&pagina=&tamanho= | todos | 200 |
| GET | /api/cafes/{id} | todos | 200, 404 |
| POST | /api/cafes | admin | 201 + Location, 400, 401, 403 |
| PUT | /api/cafes/{id} | admin | 200, 400, 401, 403, 404 |
| DELETE | /api/cafes/{id} | admin | 204, 401, 403, 404, 409 |
| POST | /api/pedidos | logado | 201 + Location, 400, 401, 404, 409 |
| GET | /api/pedidos?status=&pagina=&tamanho= | logado (cliente vê só os seus; admin, todos) | 200, 401 |
| GET | /api/pedidos/{id} | dono ou admin | 200, 401, 403, 404 |
| POST | /api/pedidos/{id}/cancelar | dono ou admin | 200, 401, 403, 404, 409 |
| GET | /health | todos | 200, 503 |
| GET | /api/versao | todos | 200 |

Todos os erros saem em **ProblemDetails** (`type`, `title`, `status`, `detail`).

## Rotas para o corretor testar (sem token)
- Listagem paginada: https://magnesium-coffee-api.onrender.com/api/cafes?pagina=1&tamanho=5
- Busca (exemplo que encontra algo): https://magnesium-coffee-api.onrender.com/api/cafes?busca=bourbon
- Busca sem resultado (200, lista vazia): https://magnesium-coffee-api.onrender.com/api/cafes?busca=zzzz
- Detalhe: https://magnesium-coffee-api.onrender.com/api/cafes/8 · inexistente (404): https://magnesium-coffee-api.onrender.com/api/cafes/9999
- Protegida (401 sem token): https://magnesium-coffee-api.onrender.com/api/pedidos

## Contas de teste (fictícias)
- admin: admin@magnesiumcoffee.com / Cafe@Caparao2026
- cliente A: ana@exemplo.com / Cliente@2026 · cliente B: bruno@exemplo.com / Cliente@2026

No Swagger: `POST /api/auth/login` → copie o `token` → botão **Authorize** → cole só o token.

## Banco de dados
PostgreSQL na **Neon**, acessado com EF Core 10 + Npgsql. Migrations versionadas em
`src/MagnesiumCoffee.Api/Dados/Migrations`, aplicadas na subida (`MigrateAsync`) junto com um seed
idempotente (admin, 2 clientes, 10 cafés e 3 pedidos inventados).

| Tabela | Principais campos |
|---|---|
| **Usuarios** | Id, Nome, Email (único), SenhaHash (BCrypt), Papel (`admin`/`cliente`), CriadoEm |
| **Cafes** (principal) | Id, Nome, Produtor, Municipio, Variedade, Processo, Torra, AltitudeMetros, PontuacaoSca, PesoGramas, Preco, **Estoque**, DataTorra, Descricao |
| **Pedidos** (segunda) | Id, **CafeId**, **UsuarioId** (dono), Quantidade, PrecoUnitario, Total, EnderecoEntrega, Status, CriadoEm |

Relação **1 Café → N Pedidos** (FK `CafeId`, `DeleteBehavior.Restrict`); cada pedido pertence ao
usuário que o criou (FK `UsuarioId`). Datas gravadas sempre em UTC.
A regra do 409 está em `src/MagnesiumCoffee.Api/Servicos/PedidoServico.cs`, método `CriarAsync`.

## Como rodar no computador
Requisitos: .NET SDK 10, `dotnet-ef` 10 e um PostgreSQL (ex.: Neon).

Variáveis de ambiente (sem valores no repositório):
- `ConnectionStrings__Padrao` — connection string do Postgres (formato Npgsql `Host=...;Database=...`
  ou a URI `postgresql://...` que a Neon mostra; a API converte)
- `Jwt__Segredo` — segredo do JWT, mínimo 32 bytes
- `Seed__AdminSenha` — senha do admin criado pelo seed
- `Cors__Origens` (opcional) — origens do front separadas por vírgula; padrão `http://localhost:5173`

```bash
dotnet tool install --global dotnet-ef --version 10.0.12
dotnet ef database update -p src/MagnesiumCoffee.Api   # opcional: a API também migra ao subir
dotnet run --project src/MagnesiumCoffee.Api            # http://localhost:5080/swagger
dotnet build -c Release -warnaserror
dotnet test                                              # 29 testes, SQLite em memória
```
Nova migration: `dotnet ef migrations add NomeDaMudanca -p src/MagnesiumCoffee.Api -o Dados/Migrations`.
Exemplos de requisições: `requisicoes.http`. Documentação de cada etapa: pasta `docs/`.

**Deploy:** `render.yaml` (Blueprint). Cada push no `main` roda o CI (`.github/workflows/ci.yml`:
build com `-warnaserror`, testes e build do front) e, se passar, a Render publica a API (Docker) e o front (site estático).

## Front-end
React 19 + Vite 8 (pasta `front/`), publicado como site estático na Render.
Telas: catálogo com busca, paginação e um perfil de altitude da Serra do Caparaó (cada café é um
ponto na altitude em que é cultivado); detalhe do café com ficha técnica e formulário de pedido
(mostra o 409 de estoque); entrar e criar conta; meus pedidos (com cancelamento; admin vê todos);
gestão de cafés para o admin (cadastrar, editar, excluir).
Na API, `Cors__Origens` = `https://magnesium-coffee-web.onrender.com,http://localhost:5173`.
Para rodar: `cd front && npm install && npm run dev` (variável `VITE_API_URL`, ver `front/.env.example`).

## Como usei a IA
- **IA usada:** Claude (Claude Code, modelo Claude Opus 5.5), no terminal, com acesso ao repositório.
- **Os 3 pedidos (prompts) que mais ajudaram:**
  1. O prompt-base da disciplina, preenchido com o nicho (Café → Pedido, 409 por estoque), pedindo
     primeiro o esqueleto no ar: DbContext, entidades, migration, `Program.cs`, Dockerfile e CI.
  2. "Não quero montar a string do Neon, quero usar a Connection String que ele já dá" — gerou o
     `ConexaoBanco.Normalizar`, que converte a URI `postgresql://` para o formato do Npgsql (com testes).
  3. "Faça todas as fatias, vá jogando para a main e testando" — cafés, pedidos com a regra do 409,
     testes de serviço e verificação de cada rota em produção.
- **Um erro da IA que percebi e como corrigi:** no esqueleto, `/health` foi feito com
  `app.MapHealthChecks("/health")`. Funcionava, mas **não aparecia no Swagger**. Corrigido em
  `src/MagnesiumCoffee.Api/Program.cs`: trocado por `app.MapGet("/health", ...)` que chama o
  `HealthCheckService` e devolve JSON (200 ou 503), agora documentado. Detalhes em `docs/02-autenticacao.md`.
- **O prompt usado para gerar o front:** "Já faça também o frontend completo. Quero um design
  moderno e responsivo." (com o contexto da API já pronta: rotas, DTOs e regras).
- **O que aprendi:**
  - **ASP.NET Core / Program.cs:** a ordem do pipeline importa (`UseCors` antes de
    `UseAuthentication`, que vem antes de `UseAuthorization`). A configuração vem de variáveis de
    ambiente, e `ConnectionStrings__Padrao` vira `ConnectionStrings:Padrao` (o `__` vira `:`), o que
    deixa os segredos fora do repositório. Atrás do proxy da Render, foi preciso ligar os forwarded
    headers para o `Location` sair com `https`.
  - **Camadas e erros:** o serviço não conhece HTTP; ele devolve `Resultado<T>` com um `TipoErro`, e
    o controller traduz para 400/401/403/404/409 com `Problem(...)`. Erro de negócio não é exceção.
    Com `AddProblemDetails` + `UseStatusCodePages`, até o 401 sem corpo sai em ProblemDetails, e o
    `[ApiController]` devolve 400 sozinho a partir das DataAnnotations dos DTOs.
  - **EF Core 10 + Npgsql + Neon:** migrations versionadas aplicadas com `MigrateAsync` na subida e
    um seed idempotente. O Npgsql exige `DateTime` com `Kind=Utc` em `timestamp with time zone`: o DTO
    recebe `DateTimeOffset` e a entidade guarda `.UtcDateTime`. A Neon entrega a connection string
    como URI (`postgresql://`), que o Npgsql não lê, por isso a conversão com
    `NpgsqlConnectionStringBuilder`.
  - **Regra de negócio e concorrência:** "ler o estoque, conferir e subtrair" falha com dois pedidos
    ao mesmo tempo. O certo é um `UPDATE` condicional (`ExecuteUpdateAsync` com `Estoque >= quantidade`)
    dentro de uma transação: se afetar 0 linhas, devolve 409 e nada é gravado.
  - **Segurança:** a senha é guardada com BCrypt (hash lento e com salt). O JWT assinado com HMAC-SHA256
    carrega o id e o papel nos claims, e a identidade sai sempre do token, nunca do corpo da requisição.
    401 é "não sei quem você é" e 403 é "sei, mas você não pode". O registro público nunca cria admin.
  - **Paginação e busca:** `Skip/Take` vira `OFFSET/LIMIT` no banco. `ToLower().Contains()` funciona
    no Postgres e no SQLite; `ILike` quebraria os testes.
  - **Swagger (Swashbuckle 10):** a API mudou para OpenAPI.NET 2.x (`OpenApiSecuritySchemeReference`).
    `MapHealthChecks` não aparece na documentação; um `MapGet` aparece. Os comentários `///` viram
    descrições.
  - **Testes com xUnit + SQLite em memória:** um banco novo por teste, com a conexão aberta, permite
    testar o 409, o 403 e a paginação nos serviços sem depender do banco da nuvem.
  - **Docker, CI e deploy:** a imagem em dois estágios (SDK para compilar, runtime para executar) fica
    menor. No GitHub Actions, `-warnaserror` transforma aviso em erro. Com o Blueprint da Render, cada
    push no `main` só é publicado se o CI passar, e o `/api/versao` mostra qual commit está no ar.
  - **Uso da IA:** ela acelera muito, mas eu precisei conferir. O `/health` gerado funcionava e não
    aparecia no Swagger, e só testando em produção eu percebi.
