import { useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { api } from '../api.js'
import useCarregar from '../useCarregar.js'
import Carregando, { Falha } from '../componentes/Carregando.jsx'
import PerfilAltitude from '../componentes/PerfilAltitude.jsx'
import CafeItem from '../componentes/CafeItem.jsx'
import Paginacao from '../componentes/Paginacao.jsx'

const TAMANHO = 9

export default function Catalogo() {
  const [parametros, setParametros] = useSearchParams()
  const busca = parametros.get('busca') ?? ''
  const pagina = Number(parametros.get('pagina') ?? 1)
  const [termo, setTermo] = useState(busca)

  const perfil = useCarregar(() => api('/api/cafes?tamanho=50'), [])
  const lista = useCarregar(
    () => api(`/api/cafes?busca=${encodeURIComponent(busca)}&pagina=${pagina}&tamanho=${TAMANHO}`),
    [busca, pagina],
  )

  function buscar(e) {
    e.preventDefault()
    setParametros(termo.trim() ? { busca: termo.trim() } : {})
  }

  function irParaPagina(n) {
    setParametros({ ...(busca && { busca }), pagina: String(n) })
    document.getElementById('catalogo')?.scrollIntoView({ behavior: 'smooth' })
  }

  return (
    <>
      <section className="abertura">
        <div className="abertura-texto">
          <h1>Café de montanha, do Caparaó para a sua xícara.</h1>
          <p>
            Compramos direto de sítios e fazendas entre 850 e 1.400 metros, nas encostas do Pico da Bandeira.
            Cada lote chega com produtor, altitude, processo e data da torra.
          </p>
          <form className="busca" role="search" onSubmit={buscar}>
            <label htmlFor="busca" className="visualmente-oculto">Buscar cafés</label>
            <input
              id="busca"
              type="search"
              value={termo}
              onChange={(e) => setTermo(e.target.value)}
              placeholder="Busque por variedade, município ou produtor"
            />
            <button className="botao botao-primario" type="submit">Buscar</button>
          </form>
        </div>
        <div className="abertura-perfil">
          {perfil.dados ? <PerfilAltitude cafes={perfil.dados.itens} /> : <div className="perfil perfil-vazio" />}
        </div>
      </section>

      <section className="pagina" id="catalogo" aria-labelledby="titulo-catalogo">
        <div className="secao-topo">
          <h2 id="titulo-catalogo">{busca ? `Resultados para “${busca}”` : 'Todos os cafés'}</h2>
          {lista.dados && (
            <p className="contagem">
              {lista.dados.total === 1 ? '1 café' : `${lista.dados.total} cafés`}
              {busca && (
                <button
                  className="link-botao"
                  onClick={() => {
                    setTermo('')
                    setParametros({})
                  }}
                >
                  Limpar busca
                </button>
              )}
            </p>
          )}
        </div>

        {lista.carregando && <Carregando texto="Buscando cafés…" />}
        {lista.erro && <Falha erro={lista.erro} aoTentarDeNovo={lista.recarregar} />}
        {lista.dados && !lista.carregando && (
          <>
            {lista.dados.itens.length === 0 ? (
              <div className="vazio">
                <p>Nenhum café encontrado para “{busca}”.</p>
                <p>Tente uma variedade (bourbon, catuaí), um processo (natural, fermentado) ou um município (Iúna, Irupi).</p>
              </div>
            ) : (
              <div className="grade-cafes">
                {lista.dados.itens.map((cafe) => (
                  <CafeItem key={cafe.id} cafe={cafe} />
                ))}
              </div>
            )}
            <Paginacao pagina={lista.dados.pagina} totalPaginas={lista.dados.totalPaginas} aoMudar={irParaPagina} />
          </>
        )}
      </section>
    </>
  )
}
