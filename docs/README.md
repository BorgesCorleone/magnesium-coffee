# Diário do projeto — Magnesium Coffee API

Registro de cada etapa (fatia vertical) do desenvolvimento, para estudar e montar o seminário.
Cada documento traz: **o que foi feito**, **por quê**, **onde está no código**, **como demonstrar
no Swagger** e **perguntas prováveis na arguição**.

| # | Etapa | Documento |
|---|---|---|
| 1 | Esqueleto no ar (domínio, banco, Program.cs, Docker, CI, Render) | [01-esqueleto.md](01-esqueleto.md) |
| 2 | Autenticação (registro, login, JWT, papéis) e /health no Swagger | [02-autenticacao.md](02-autenticacao.md) |
| 3 | Cafés: catálogo paginado com busca e CRUD do admin | [03-cafes.md](03-cafes.md) |
| 4 | Pedidos e a regra do 409 (estoque) | [04-pedidos.md](04-pedidos.md) |
| 5 | Front-end React + Vite | [05-front-end.md](05-front-end.md) |
| — | Roteiro do seminário (10 min + arguição) | [roteiro-seminario.md](roteiro-seminario.md) |

## Mapa rápido do código
```
src/MagnesiumCoffee.Api/
├─ Program.cs                  configuração de tudo (banco, JWT, Swagger, CORS, erros, health, seed)
├─ Dominio/                    entidades com regras (Usuario, Cafe, Pedido) — sem dependência de EF/HTTP
├─ Dados/                      AppDbContext, Migrations, Seed, ConexaoBanco (URI da Neon → Npgsql)
├─ Servicos/                   regras de negócio; devolvem Resultado<T> em vez de lançar exceção
├─ Dtos/                       o que entra e sai pela API (com DataAnnotations)
└─ Controllers/                traduzem HTTP ↔ serviço; ApiBaseController converte TipoErro em status
tests/MagnesiumCoffee.Tests/   xUnit; SQLite em memória, um banco novo por teste
```

## Fluxo de uma requisição (para explicar no seminário)
```
HTTP → [CORS] → [Autenticação JWT] → [Autorização / Roles] → Controller
     → validação automática dos DTOs ([ApiController] → 400)
     → Serviço (regra de negócio, EF Core) → Resultado<T>
     → Controller: sucesso → 200/201/204 · falha → Problem(...) 400/401/403/404/409
```
