# 03 — Cafés (entidade principal)

**Objetivo:** catálogo público paginado com busca e CRUD completo restrito ao admin.

## O que foi feito
| Item | Arquivo |
|---|---|
| DTOs de entrada/saída com validação | `Dtos/CafeDtos.cs` |
| Página genérica `{ itens, pagina, tamanho, total, totalPaginas }` | `Dtos/PaginaDto.cs` |
| Regras | `Servicos/CafeServico.cs` |
| Endpoints | `Controllers/CafesController.cs` |
| Conversão de data para a saída | `Dominio/Datas.cs` (`ComoUtc`) |
| Testes | `tests/.../CafeServicoTests.cs` (8), `tests/.../Fabrica.cs` |

## Endpoints
| Verbo | Rota | Quem pode | Respostas |
|---|---|---|---|
| GET | `/api/cafes?busca=&pagina=1&tamanho=10` | todos | 200 |
| GET | `/api/cafes/{id}` | todos | 200, 404 |
| POST | `/api/cafes` | admin | 201 (+ Location), 400, 401, 403 |
| PUT | `/api/cafes/{id}` | admin | 200, 400, 401, 403, 404 |
| DELETE | `/api/cafes/{id}` | admin | 204, 401, 403, 404, 409 |

## Decisões e por quê

### Listagem paginada com busca (fato técnico 9)
- `busca` procura em nome, produtor, município, variedade e processo com
  `ToLower().Contains(termo)` — funciona no Postgres **e** no SQLite dos testes
  (`EF.Functions.ILike` só existe no Postgres e quebraria os testes).
- `pagina` mínimo 1 e `tamanho` entre 1 e 50 (`Paginacao.Normalizar`): ninguém pede 1 milhão de linhas.
- Ordem por nome, `Skip/Take` no banco (não traz tudo para a memória).
- Busca sem resultado → **200** com `itens: []`, `total: 0`, `totalPaginas: 0` (não é erro).

### Autorização
`[Authorize(Roles = "admin")]` em POST, PUT e DELETE. Sem token → **401**; token de cliente → **403**.
GET é público (vitrine da loja).

### 201 + Location
`CreatedAtAction(nameof(ObterPorId), new { id }, dto)` devolve 201 com o cabeçalho
`Location: /api/cafes/{id}`. O CORS expõe esse cabeçalho (`WithExposedHeaders("Location")`) para o front.

### Datas
O DTO recebe `DataTorra` como `DateTimeOffset` (ex.: `2026-10-20T09:00:00-03:00`); o serviço grava
`.UtcDateTime` (12:00 UTC). Data da torra no futuro → **400** (`TipoErro.Validacao`), uma validação
que depende de "agora" e por isso fica no serviço, não em atributo.

### Excluir com pedidos → 409
Um café já vendido não pode sumir: os pedidos antigos apontam para ele. O serviço confere antes
(`Pedidos.AnyAsync`) e devolve **409** com a orientação "zere o estoque para tirá-lo de venda".
O banco também protege (`DeleteBehavior.Restrict`).

### Swagger com descrições
`GenerateDocumentationFile` no `.csproj` + `IncludeXmlComments` no `Program.cs`: os comentários
`/// <summary>` viram a descrição de cada rota. O aviso CS1591 (membro público sem comentário) foi
silenciado porque o build usa `-warnaserror`.

## Testes
1. Paginação devolve a página certa e calcula `totalPaginas`.
2. Busca ignora maiúsculas e procura no município.
3. Busca sem resultado → lista vazia e total 0.
4. Id inexistente → `NaoEncontrado`.
5. Data com fuso -03:00 é gravada em UTC (09h → 12h).
6. Data da torra no futuro → `Validacao`.
7. Excluir café com pedidos → `Conflito`.
8. Excluir café sem pedidos → removido.

## Como demonstrar no Swagger
1. `GET /api/cafes?pagina=1&tamanho=5` → 5 itens, `total` 10, `totalPaginas` 2.
2. `GET /api/cafes?busca=bourbon` → encontra; `busca=zzzz` → lista vazia.
3. `GET /api/cafes/9999` → 404 em ProblemDetails.
4. Sem token, `POST /api/cafes` → 401. Com token da Ana (cliente) → 403.
5. Com token do admin, `POST` → 201 e o header `location`. Corpo inválido → 400 com erros por campo.
6. `DELETE /api/cafes/1` (tem pedidos do seed) → 409.

## Perguntas prováveis
- *Por que paginar no banco?* `Skip/Take` vira `OFFSET/LIMIT` no SQL; só a página trafega.
- *Diferença entre 401 e 403?* 401: não sei quem você é (sem token/token inválido). 403: sei quem
  você é, mas você não pode.
- *Por que não `ILike`?* Os testes usam SQLite, que não tem `ILIKE`.
