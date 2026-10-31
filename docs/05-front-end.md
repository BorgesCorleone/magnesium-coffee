# 05 — Front-end (React + Vite)

**Objetivo:** provar que a API serve um cliente de verdade. O front não é apresentado no
seminário, mas vale nota (2 pontos) e precisa estar no ar.

## Stack
React 19.3, React Router 7.18, Vite 8.3, JavaScript (JSX). Sem biblioteca de componentes: CSS próprio.
Publicado como **Static Site** na Render (`magnesium-coffee-web` no `render.yaml`).

## Telas
| Rota | Tela | Usa |
|---|---|---|
| `/` | Catálogo: busca, paginação e o **perfil de altitude** da Serra do Caparaó | `GET /api/cafes` |
| `/cafes/:id` | Detalhe com ficha técnica e formulário de pedido | `GET /api/cafes/{id}`, `POST /api/pedidos` |
| `/entrar`, `/criar-conta` | Login e cadastro | `POST /api/auth/login`, `/registro` |
| `/pedidos` | Meus pedidos (admin: todos), filtro e cancelamento | `GET /api/pedidos`, `POST .../cancelar` |
| `/admin` | Lista de cafés com editar/excluir | `GET`, `DELETE /api/cafes` |
| `/admin/cafes/novo`, `/admin/cafes/:id` | Formulário de café | `POST`, `PUT /api/cafes` |

## Como conversa com a API
- `src/api.js`: `fetch` com `VITE_API_URL`; erro HTTP vira `ErroApi` com a mensagem do
  `detail` do ProblemDetails e os erros por campo (`errors`) para mostrar embaixo de cada input.
- `src/sessao.jsx`: guarda `{ token, usuario }` no `localStorage`; manda `Authorization: Bearer`;
  se a API responder 401, encerra a sessão.
- O 409 de estoque aparece em destaque no formulário de pedido, com a mensagem da API.
- Primeira visita com a API "dormindo" (instância Free): após 4 s a tela explica que o servidor está acordando.

## CORS
A API só aceita chamadas das origens em `Cors__Origens`:
`https://magnesium-coffee-web.onrender.com,http://localhost:5173` (definido no `render.yaml`).

## Design
- Elemento principal: o **perfil de altitude** — a crista da serra até o Pico da Bandeira (2.892 m),
  com cada café plotado na altitude em que é cultivado; clicar abre o café.
- Cores da serra (verde-ardósia, névoa) com um único acento: o vermelho da cereja do café.
- Tipografia: Young Serif (títulos) e Schibsted Grotesk (texto).
- Torra mostrada como três grãos que escurecem; cada card tem uma régua lateral de altitude.
- Responsivo (grade fluida, cabeçalho em uma linha no celular), foco visível no teclado,
  `prefers-reduced-motion` respeitado.

## Rodar localmente
```bash
cd front
npm install
npm run dev          # http://localhost:5173 (API em VITE_API_URL ou http://localhost:5080)
```
