import { createContext, useCallback, useContext, useMemo, useState } from 'react'
import { api } from './api.js'

const CHAVE = 'magnesium.sessao'
const SessaoContexto = createContext(null)

function lerSessao() {
  try {
    const sessao = JSON.parse(localStorage.getItem(CHAVE))
    if (sessao && new Date(sessao.expiraEm) > new Date()) return sessao
  } catch {
    /* sessão corrompida ou storage bloqueado: começa deslogado */
  }
  return null
}

function gravarSessao(sessao) {
  try {
    if (sessao) localStorage.setItem(CHAVE, JSON.stringify(sessao))
    else localStorage.removeItem(CHAVE)
  } catch {
    /* sem storage: a sessão vale só enquanto a aba estiver aberta */
  }
}

export function SessaoProvider({ children }) {
  const [sessao, setSessao] = useState(lerSessao)

  const salvar = useCallback((nova) => {
    gravarSessao(nova)
    setSessao(nova)
  }, [])

  const entrar = useCallback(
    async (email, senha) => salvar(await api('/api/auth/login', { metodo: 'POST', corpo: { email, senha } })),
    [salvar],
  )

  const registrar = useCallback(
    async (nome, email, senha) =>
      salvar(await api('/api/auth/registro', { metodo: 'POST', corpo: { nome, email, senha } })),
    [salvar],
  )

  const sair = useCallback(() => salvar(null), [salvar])

  // Chamada autenticada: se o token expirou (401), encerra a sessão.
  const chamar = useCallback(
    async (caminho, opcoes = {}) => {
      try {
        return await api(caminho, { ...opcoes, token: sessao?.token })
      } catch (erro) {
        if (erro.status === 401 && sessao) salvar(null)
        throw erro
      }
    },
    [sessao, salvar],
  )

  const valor = useMemo(
    () => ({
      usuario: sessao?.usuario ?? null,
      logado: !!sessao,
      ehAdmin: sessao?.usuario?.papel === 'admin',
      entrar,
      registrar,
      sair,
      chamar,
    }),
    [sessao, entrar, registrar, sair, chamar],
  )

  return <SessaoContexto.Provider value={valor}>{children}</SessaoContexto.Provider>
}

export const useSessao = () => useContext(SessaoContexto)
