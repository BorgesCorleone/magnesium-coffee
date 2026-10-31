// Cliente HTTP da API. Erros chegam em ProblemDetails e viram ErroApi com uma mensagem legível.
export const API_URL = (import.meta.env.VITE_API_URL ?? 'http://localhost:5080').replace(/\/$/, '')

export class ErroApi extends Error {
  constructor(status, problema) {
    super(mensagemDe(status, problema))
    this.status = status
    this.campos = normalizarCampos(problema?.errors)
  }
}

const PADRAO = {
  400: 'Confira os dados informados.',
  401: 'Sua sessão expirou. Entre novamente.',
  403: 'Você não tem permissão para fazer isso.',
  404: 'Não encontramos o que você procurou.',
  409: 'Não foi possível concluir por um conflito com os dados atuais.',
}

function mensagemDe(status, problema) {
  if (problema?.detail) return problema.detail
  const campos = Object.values(problema?.errors ?? {}).flat()
  if (campos.length) return campos[0]
  return PADRAO[status] ?? 'Algo deu errado no servidor. Tente de novo em instantes.'
}

// A API devolve as chaves dos campos como "Nome", "EnderecoEntrega"...; o front usa camelCase.
function normalizarCampos(errors) {
  const campos = {}
  for (const [chave, mensagens] of Object.entries(errors ?? {})) {
    const nome = chave.replace(/^\$\./, '').replace(/^dto\./i, '')
    campos[nome.charAt(0).toLowerCase() + nome.slice(1)] = mensagens[0]
  }
  return campos
}

export async function api(caminho, { metodo = 'GET', corpo, token } = {}) {
  let resposta
  try {
    resposta = await fetch(API_URL + caminho, {
      method: metodo,
      headers: {
        Accept: 'application/json',
        ...(corpo !== undefined && { 'Content-Type': 'application/json' }),
        ...(token && { Authorization: `Bearer ${token}` }),
      },
      body: corpo !== undefined ? JSON.stringify(corpo) : undefined,
    })
  } catch {
    throw new ErroApi(0, { detail: 'Sem conexão com a API. Verifique sua internet e tente de novo.' })
  }

  if (resposta.status === 204) return null
  const texto = await resposta.text()
  const dados = texto ? JSON.parse(texto) : null
  if (!resposta.ok) throw new ErroApi(resposta.status, dados)
  return dados
}

const moeda = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })
const numero = new Intl.NumberFormat('pt-BR')
const data = new Intl.DateTimeFormat('pt-BR', { day: '2-digit', month: 'short', year: 'numeric' })

export const formatar = {
  preco: (v) => moeda.format(v),
  altitude: (m) => `${numero.format(m)} m`,
  peso: (g) => (g >= 1000 ? `${numero.format(g / 1000)} kg` : `${g} g`),
  data: (iso) => data.format(new Date(iso)),
  nota: (n) => numero.format(n),
}

export const TORRAS = { clara: 'Clara', media: 'Média', escura: 'Escura' }
