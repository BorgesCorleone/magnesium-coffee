import { Link, NavLink, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { API_URL } from '../api.js'
import { useSessao } from '../sessao.jsx'

export function Cabecalho() {
  const { usuario, logado, ehAdmin, sair } = useSessao()
  const navegar = useNavigate()

  return (
    <header className="cabecalho">
      <div className="cabecalho-interno">
        <Link to="/" className="marca" aria-label="Magnesium Coffee, página inicial">
          <svg viewBox="0 0 40 24" aria-hidden="true">
            <path d="M1 22 L12 9 L17 14 L26 2 L39 22 Z" />
            <circle cx="26" cy="4.5" r="3" />
          </svg>
          <span>Magnesium Coffee</span>
        </Link>

        <nav className="menu" aria-label="Principal">
          <NavLink to="/" end>Cafés</NavLink>
          {logado && <NavLink to="/pedidos">{ehAdmin ? 'Pedidos' : 'Meus pedidos'}</NavLink>}
          {ehAdmin && <NavLink to="/admin">Gerenciar cafés</NavLink>}
        </nav>

        <div className="conta">
          {logado ? (
            <>
              <span className="conta-nome" title={usuario.email}>
                {usuario.nome.split(' ')[0]}
                {ehAdmin && <span className="selo">admin</span>}
              </span>
              <button
                className="botao botao-fantasma"
                onClick={() => {
                  sair()
                  navegar('/')
                }}
              >
                Sair
              </button>
            </>
          ) : (
            <>
              <NavLink to="/entrar" className="botao botao-fantasma">Entrar</NavLink>
              <NavLink to="/criar-conta" className="botao botao-primario so-largo">Criar conta</NavLink>
            </>
          )}
        </div>
      </div>
    </header>
  )
}

export function Rodape() {
  return (
    <footer className="rodape">
      <div className="rodape-interno">
        <p>
          <strong>Magnesium Coffee</strong> é uma empresa fictícia, criada para um projeto escolar. Cafés, produtores e
          pedidos são inventados; nenhuma compra é cobrada.
        </p>
        <p>
          <a href={`${API_URL}/swagger`} target="_blank" rel="noreferrer">Documentação da API</a>
        </p>
      </div>
    </footer>
  )
}

// Rotas que exigem login (e, opcionalmente, papel admin).
export function Protegida({ admin = false, children }) {
  const { logado, ehAdmin } = useSessao()
  const local = useLocation()
  if (!logado) return <Navigate to="/entrar" replace state={{ voltarPara: local.pathname }} />
  if (admin && !ehAdmin)
    return (
      <section className="pagina pagina-estreita">
        <h1>Área do administrador</h1>
        <p>Sua conta é de cliente e não pode gerenciar o catálogo.</p>
        <Link to="/" className="botao botao-secundario">Ver cafés</Link>
      </section>
    )
  return children
}
