# 04 — Pedidos (segunda entidade) e a regra do 409

**Objetivo:** cliente compra pacotes de um café; nunca vender mais do que há em estoque.

## O que foi feito
| Item | Arquivo |
|---|---|
| DTOs | `Dtos/PedidoDtos.cs` |
| Regras (inclui a do 409) | `Servicos/PedidoServico.cs` |
| Endpoints | `Controllers/PedidosController.cs` |
| Pedidos de exemplo | `Dados/Seed.cs` |
| Testes | `tests/.../PedidoServicoTests.cs` (9) |

## Endpoints (todos exigem login → 401 sem token)
| Verbo | Rota | Quem pode | Respostas |
|---|---|---|---|
| POST | `/api/pedidos` | logado | 201, 400, 401, 404, **409** |
| GET | `/api/pedidos?status=&pagina=&tamanho=` | logado (cliente vê só os seus) | 200, 401 |
| GET | `/api/pedidos/{id}` | dono ou admin | 200, 401, 403, 404 |
| POST | `/api/pedidos/{id}/cancelar` | dono ou admin | 200, 401, 403, 404, 409 |

## A regra de negócio do 409 (fato técnico 8)
**Quantidade pedida maior que o estoque do café → 409 Conflict**, com mensagem clara no `detail`:
`Estoque insuficiente para "Ibitirama Geisha": pedido de 10 pacote(s), disponível 6.`

Onde: `PedidoServico.CriarAsync`. Passo a passo:
1. `BeginTransactionAsync()` abre a transação.
2. Busca o café; não existe → 404.
3. Baixa **atômica** do estoque, num único `UPDATE` condicional:
   ```csharp
   db.Cafes.Where(c => c.Id == id && c.Estoque >= quantidade)
           .ExecuteUpdateAsync(s => s.SetProperty(c => c.Estoque, c => c.Estoque - quantidade));
   ```
   SQL gerado: `UPDATE "Cafes" SET "Estoque" = "Estoque" - @q WHERE "Id" = @id AND "Estoque" >= @q`.
4. Se o `UPDATE` afetou **0 linhas**, não havia estoque → `Rollback` + **409**.
5. Senão, grava o pedido (com o preço copiado do café) e faz `Commit`.

**Por que o UPDATE condicional e não "ler, conferir, subtrair"?** Se dois clientes pedirem os
últimos 6 pacotes ao mesmo tempo, os dois leriam "estoque 6" e os dois comprariam (estoque -6).
Com a condição `Estoque >= quantidade` dentro do próprio `UPDATE`, o banco trava a linha: o
primeiro baixa para 0 e o segundo afeta 0 linhas → 409. A transação garante que estoque e pedido
mudam juntos (ou nenhum muda).

## Dono e autorização (fato técnico 7)
- O dono do pedido é **sempre** o `UsuarioId` do token (`ApiBaseController.UsuarioId`); o DTO nem tem esse campo.
- Listagem: cliente → `Where(p => p.UsuarioId == usuarioId)`; admin vê todos.
- Detalhe e cancelamento: o serviço confere **dono-ou-admin** antes; outro cliente → **403**.
- Cancelar devolve os pacotes ao estoque (também com `UPDATE` atômico) e muda o status para
  `cancelado`; cancelar de novo → **409**.
- Não há DELETE de pedido: em comércio, pedido não some, é cancelado (fica o histórico).

## Saída sem ciclos
A listagem projeta direto para `PedidoSaidaDto` (`Select`), trazendo `CafeNome` e `ClienteNome`
num só SQL com JOIN. Não devolvemos entidades do EF (evita ciclo Pedido → Cafe → Pedidos e vazar `SenhaHash`).

## Logs estruturados
- `Pedido {PedidoId} criado: usuário {UsuarioId}, café {CafeId}, {Quantidade} un., total {Total}`
- `Pedido recusado por estoque: café {CafeId}, pedido {Quantidade}, disponível {Disponivel}, usuário {UsuarioId}` (Warning)
- `Usuário {UsuarioId} tentou cancelar o pedido {PedidoId} de outro cliente` (Warning)

## Testes
1. Criar baixa o estoque e copia o preço (total = 2 × 50).
2. **Quantidade > estoque → Conflito**, estoque intacto e nenhum pedido gravado.
3. **Segundo pedido que esgota → Conflito** (4 de 5, depois 2 → sobra 1).
4. Café inexistente → NaoEncontrado.
5. **Cliente lista só os seus**; admin lista todos.
6. **Cliente não vê pedido de outro → Proibido.**
7. **Cliente não cancela pedido de outro → Proibido** (estoque não muda).
8. Admin cancela pedido de cliente e o estoque volta.
9. Cancelar já cancelado → Conflito.

## Como demonstrar no Swagger
1. Login como `ana@exemplo.com` → Authorize.
2. `GET /api/cafes/8` → "Ibitirama Geisha", estoque 6.
3. `POST /api/pedidos` `{ "cafeId": 8, "quantidade": 10, "enderecoEntrega": "Rua das Flores, 100 — Guaçuí/ES" }` → **409**.
4. Mesmo corpo com `"quantidade": 2` → **201**; `GET /api/cafes/8` → estoque 4.
5. `GET /api/pedidos` → só pedidos da Ana.
6. Login como `bruno@exemplo.com`, `POST /api/pedidos/{id da Ana}/cancelar` → **403**.
7. Ana cancela o próprio → 200 e estoque volta a 6.

## Perguntas prováveis
- *Onde está a regra do 409?* `PedidoServico.CriarAsync`, no `ExecuteUpdateAsync` com `Estoque >= quantidade`.
- *Por que transação?* Baixa de estoque e criação do pedido são uma operação só: ou as duas, ou nenhuma.
- *E se dois pedidos chegarem juntos?* O `UPDATE` condicional é atômico no banco; o segundo recebe 409.
- *Por que copiar o preço?* Se o admin reajustar o café, o valor do pedido antigo não muda.
