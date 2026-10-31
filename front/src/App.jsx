import { Link, Route, Routes } from 'react-router-dom'
import { Cabecalho, Protegida, Rodape } from './componentes/Estrutura.jsx'
import Catalogo from './paginas/Catalogo.jsx'
import CafeDetalhe from './paginas/CafeDetalhe.jsx'
import { CriarConta, Entrar } from './paginas/Acesso.jsx'
import Pedidos from './paginas/Pedidos.jsx'
import { AdminCafeFormulario, AdminCafes } from './paginas/Admin.jsx'

export default function App() {
  return (
    <>
      <a href="#conteudo" className="pular">Ir para o conteúdo</a>
      <Cabecalho />
      <main id="conteudo">
        <Routes>
          <Route path="/" element={<Catalogo />} />
          <Route path="/cafes/:id" element={<CafeDetalhe />} />
          <Route path="/entrar" element={<Entrar />} />
          <Route path="/criar-conta" element={<CriarConta />} />
          <Route path="/pedidos" element={<Protegida><Pedidos /></Protegida>} />
          <Route path="/admin" element={<Protegida admin><AdminCafes /></Protegida>} />
          <Route path="/admin/cafes/novo" element={<Protegida admin><AdminCafeFormulario /></Protegida>} />
          <Route path="/admin/cafes/:id" element={<Protegida admin><AdminCafeFormulario /></Protegida>} />
          <Route
            path="*"
            element={
              <section className="pagina pagina-estreita">
                <h1>Página não encontrada</h1>
                <p>O endereço pode ter mudado ou sido digitado errado.</p>
                <Link to="/" className="botao botao-secundario">Ver cafés</Link>
              </section>
            }
          />
        </Routes>
      </main>
      <Rodape />
    </>
  )
}
