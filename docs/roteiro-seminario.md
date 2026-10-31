# Roteiro do seminário (10 min + até 5 min de arguição)

O seminário é **só do back-end**, com demonstração ao vivo no Swagger publicado.
**10 minutos antes:** abra https://magnesium-coffee-api.onrender.com/health para "acordar" a
instância Free (a primeira resposta leva até 1 minuto).

## Sugestão de slides e tempo
| Tempo | Slide / ação | O que falar |
|---|---|---|
| 0:00–1:00 | O negócio | Loja de cafés de altitude do Caparaó; admin cuida do catálogo, cliente faz pedidos. Regra: não vender além do estoque (409). |
| 1:00–2:00 | Arquitetura | Stack (.NET 10, EF Core + Npgsql, Neon, JWT/BCrypt, Swagger, xUnit, Docker, GitHub Actions, Render). Camadas: Controller → Serviço → EF Core; `Resultado<T>` em vez de exceção. |
| 2:00–3:00 | Banco | Usuarios, Cafes (principal), Pedidos (segunda, 1 Café → N Pedidos, com dono). Migrations + seed. Datas em UTC. |
| 3:00–7:00 | **Demo ao vivo no Swagger** | Roteiro abaixo. |
| 7:00–8:30 | **Caminho no código** | Seguir o POST /api/pedidos (abaixo). |
| 8:30–9:15 | **Um teste** | `Quantidade_maior_que_estoque_devolve_conflito_e_nao_mexe_no_estoque` em `tests/MagnesiumCoffee.Tests/PedidoServicoTests.cs`. |
| 9:15–10:00 | **Erro da IA** + entrega contínua | `/health` não aparecia no Swagger (MapHealthChecks → MapGet). Push no main → CI → Render (`/api/versao` mostra o commit). |

## Demo ao vivo (Swagger)
1. `GET /api/versao` → mostra o commit (prova do deploy automático).
2. `GET /api/cafes?pagina=1&tamanho=5` → paginação; `?busca=bourbon` → busca; `?busca=zzzz` → lista vazia.
3. `GET /api/cafes/9999` → **404** em ProblemDetails.
4. `GET /api/pedidos` sem token → **401**.
5. `POST /api/auth/login` com `ana@exemplo.com` / `Cliente@2026` → copiar token → **Authorize**.
6. `POST /api/cafes` como Ana → **403** (só admin).
7. `POST /api/pedidos` `{ "cafeId": 8, "quantidade": 50, "enderecoEntrega": "Rua das Flores, 100 - Guaçuí/ES" }` → **409** "Estoque insuficiente… disponível 6".
8. Mesmo pedido com `"quantidade": 2` → **201** + Location; `GET /api/cafes/8` → estoque 4.
9. `POST /api/pedidos` com `{ "cafeId": 0, "quantidade": 0, "enderecoEntrega": "" }` → **400** com erros por campo.
10. (Se der tempo) login como admin → `POST /api/cafes` → **201**; `DELETE /api/cafes/1` → **409** (tem pedidos).
11. Cancelar o pedido do passo 8 (`POST /api/pedidos/{id}/cancelar`) para devolver o estoque.

## Caminho no código: POST /api/pedidos
1. `Program.cs` — pipeline: `UseCors` → `UseAuthentication` → `UseAuthorization` → `MapControllers`.
2. `Controllers/PedidosController.cs` — `[Authorize]` na classe; `Criar(PedidoEntradaDto dto)`.
   `[ApiController]` valida as DataAnnotations do DTO antes (400 automático).
3. `Controllers/ApiBaseController.cs` — `UsuarioId` vem do claim `NameIdentifier` do token.
4. `Servicos/PedidoServico.cs` — `CriarAsync`: transação → `ExecuteUpdateAsync` com
   `Estoque >= quantidade` → 0 linhas = `Resultado.Falha(TipoErro.Conflito, ...)`.
5. De volta ao controller: `Erro(resultado)` traduz `Conflito` → `Problem(statusCode: 409, detail: ...)`;
   sucesso → `CreatedAtAction(...)` = 201 + Location.

## Perguntas prováveis na arguição
- **Onde fica a regra do 409 e por que numa transação?** `PedidoServico.CriarAsync`; baixa de estoque
  e gravação do pedido acontecem juntas ou nenhuma acontece.
- **E se dois clientes comprarem o último pacote ao mesmo tempo?** O `UPDATE ... WHERE Estoque >= q`
  é atômico: o segundo afeta 0 linhas e recebe 409.
- **Como a API sabe quem é o usuário?** Pelos claims do JWT assinado (`NameIdentifier`, `Role`);
  nunca pelo corpo da requisição.
- **Diferença entre 401 e 403?** 401 = não autenticado; 403 = autenticado sem permissão.
- **Por que BCrypt?** Hash lento e com salt, contra força bruta e tabelas prontas.
- **Por que `DateTimeOffset` no DTO?** O Npgsql exige UTC em `timestamp with time zone`; o DTO aceita
  qualquer fuso e a entidade guarda `.UtcDateTime` (o construtor recusa outra coisa).
- **Por que os testes usam SQLite?** Rápido, em memória, um banco novo por teste, sem depender da
  Neon; por isso a busca usa `ToLower().Contains()` e não `ILike`.
- **Onde estão os segredos?** Variáveis de ambiente na Render; nada no repositório.
- **Como funciona o deploy?** Push no main → GitHub Actions (build `-warnaserror` + testes) →
  Render só publica se o CI passar (`autoDeployTrigger: checksPass`).
- **O que é ProblemDetails?** Formato padrão de erro HTTP (RFC 9457/7807) com `type`, `title`,
  `status`, `detail`; vem de `AddProblemDetails` + `Problem(...)` nos controllers.
