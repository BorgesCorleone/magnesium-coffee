import { useState } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { useSessao } from '../sessao.jsx'
import { useAviso } from '../componentes/Aviso.jsx'
import Campo from '../componentes/Campo.jsx'

function useFormulario(enviar) {
  const [enviando, setEnviando] = useState(false)
  const [erro, setErro] = useState(null)
  const navegar = useNavigate()
  const local = useLocation()

  async function aoEnviar(e) {
    e.preventDefault()
    setEnviando(true)
    setErro(null)
    try {
      await enviar()
      navegar(local.state?.voltarPara ?? '/', { replace: true })
    } catch (falha) {
      setErro(falha)
    } finally {
      setEnviando(false)
    }
  }
  return { enviando, erro, aoEnviar }
}

export function Entrar() {
  const { entrar, logado } = useSessao()
  const avisar = useAviso()
  const local = useLocation()
  const [email, setEmail] = useState('')
  const [senha, setSenha] = useState('')
  const { enviando, erro, aoEnviar } = useFormulario(async () => {
    const sessao = await entrar(email, senha)
    avisar(`Olá, ${sessao?.usuario?.nome?.split(' ')[0] ?? 'de novo'}!`)
  })

  if (logado && !enviando) return <Navigate to={local.state?.voltarPara ?? '/'} replace />

  return (
    <section className="pagina pagina-estreita acesso">
      <h1>Entrar</h1>
      <form onSubmit={aoEnviar} noValidate>
        <Campo rotulo="E-mail" type="email" autoComplete="email" value={email} onChange={(e) => setEmail(e.target.value)} erro={erro?.campos?.email} />
        <Campo rotulo="Senha" type="password" autoComplete="current-password" value={senha} onChange={(e) => setSenha(e.target.value)} erro={erro?.campos?.senha} />
        {erro && !Object.keys(erro.campos).length && <p className="mensagem mensagem-erro" role="alert">{erro.message}</p>}
        <button className="botao botao-primario botao-largo" disabled={enviando}>{enviando ? 'Entrando…' : 'Entrar'}</button>
      </form>
      <div className="contas-teste">
        <p>Contas fictícias para testar:</p>
        <button
          type="button"
          className="link-botao"
          onClick={() => {
            setEmail('ana@exemplo.com')
            setSenha('Cliente@2026')
          }}
        >
          Preencher com a cliente Ana
        </button>
      </div>
      <p className="acesso-troca">Ainda não tem conta? <Link to="/criar-conta" state={local.state}>Criar conta</Link></p>
    </section>
  )
}

export function CriarConta() {
  const { registrar, logado } = useSessao()
  const avisar = useAviso()
  const local = useLocation()
  const [nome, setNome] = useState('')
  const [email, setEmail] = useState('')
  const [senha, setSenha] = useState('')
  const { enviando, erro, aoEnviar } = useFormulario(async () => {
    await registrar(nome, email, senha)
    avisar('Conta criada. Bem-vindo à Magnesium Coffee!')
  })

  if (logado && !enviando) return <Navigate to={local.state?.voltarPara ?? '/'} replace />

  return (
    <section className="pagina pagina-estreita acesso">
      <h1>Criar conta</h1>
      <form onSubmit={aoEnviar} noValidate>
        <Campo rotulo="Nome" autoComplete="name" value={nome} onChange={(e) => setNome(e.target.value)} erro={erro?.campos?.nome} />
        <Campo rotulo="E-mail" type="email" autoComplete="email" value={email} onChange={(e) => setEmail(e.target.value)} erro={erro?.campos?.email} />
        <Campo rotulo="Senha" type="password" autoComplete="new-password" value={senha} onChange={(e) => setSenha(e.target.value)} erro={erro?.campos?.senha} dica="Pelo menos 8 caracteres." />
        {erro && !Object.keys(erro.campos).length && <p className="mensagem mensagem-erro" role="alert">{erro.message}</p>}
        <button className="botao botao-primario botao-largo" disabled={enviando}>{enviando ? 'Criando conta…' : 'Criar conta'}</button>
      </form>
      <p className="acesso-troca">Já tem conta? <Link to="/entrar" state={local.state}>Entrar</Link></p>
    </section>
  )
}
