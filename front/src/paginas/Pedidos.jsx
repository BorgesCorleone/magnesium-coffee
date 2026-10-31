import { useState } from 'react'
import { Link } from 'react-router-dom'
import { formatar } from '../api.js'
import { useSessao } from '../sessao.jsx'
import { useAviso } from '../componentes/Aviso.jsx'
import useCarregar from '../useCarregar.js'
import Carregando, { Falha } from '../componentes/Carregando.jsx'
import Paginacao from '../componentes/Paginacao.jsx'

const FILTROS = [
  ['', 'Todos'],
  ['pendente', 'Pendentes'],
  ['cancelado', 'Cancelados'],
]

export default function Pedidos() {
  const { chamar, ehAdmin } = useSessao()
  const [status, setStatus] = useState('')
  const [pagina, setPagina] = useState(1)
  const lista = useCarregar(() => chamar(`/api/pedidos?status=${status}&pagina=${pagina}&tamanho=10`), [status, pagina])

  return (
    <section className="pagina">
      <div className="secao-topo">
        <h1>{ehAdmin ? 'Pedidos de todos os clientes' : 'Meus pedidos'}</h1>
        <div className="filtros" role="group" aria-label="Filtrar por situação">
          {FILTROS.map(([valor, rotulo]) => (
            <button
              key={valor}
              className={status === valor ? 'filtro filtro-ativo' : 'filtro'}
              aria-pressed={status === valor}
              onClick={() => {
                setStatus(valor)
                setPagina(1)
              }}
            >
              {rotulo}
            </button>
          ))}
        </div>
      </div>

      {lista.carregando && !lista.dados && <Carregando texto="Buscando pedidos…" />}
      {lista.erro && <Falha erro={lista.erro} aoTentarDeNovo={lista.recarregar} />}
      {lista.dados &&
        (lista.dados.itens.length === 0 ? (
          <div className="vazio">
            <p>{status ? 'Nenhum pedido com essa situação.' : 'Você ainda não fez nenhum pedido.'}</p>
            <Link to="/" className="botao botao-primario">Escolher um café</Link>
          </div>
        ) : (
          <>
            <ul className="lista-pedidos">
              {lista.dados.itens.map((pedido) => (
                <LinhaPedido key={pedido.id} pedido={pedido} mostrarCliente={ehAdmin} aoMudar={lista.recarregar} />
              ))}
            </ul>
            <Paginacao pagina={lista.dados.pagina} totalPaginas={lista.dados.totalPaginas} aoMudar={setPagina} />
          </>
        ))}
    </section>
  )
}

function LinhaPedido({ pedido, mostrarCliente, aoMudar }) {
  const { chamar } = useSessao()
  const avisar = useAviso()
  const [confirmando, setConfirmando] = useState(false)
  const [enviando, setEnviando] = useState(false)
  const [erro, setErro] = useState(null)

  async function cancelar() {
    setEnviando(true)
    setErro(null)
    try {
      await chamar(`/api/pedidos/${pedido.id}/cancelar`, { metodo: 'POST' })
      avisar(`Pedido nº ${pedido.id} cancelado. Os pacotes voltaram ao estoque.`)
      aoMudar()
    } catch (falha) {
      setErro(falha)
    } finally {
      setEnviando(false)
      setConfirmando(false)
    }
  }

  const cancelado = pedido.status === 'cancelado'

  return (
    <li className={`pedido${cancelado ? ' pedido-cancelado' : ''}`}>
      <div className="pedido-principal">
        <p className="pedido-numero">Pedido nº {pedido.id} · {formatar.data(pedido.criadoEm)}</p>
        <h2>
          <Link to={`/cafes/${pedido.cafeId}`}>{pedido.cafeNome}</Link>
        </h2>
        <p className="pedido-detalhe">
          {pedido.quantidade} {pedido.quantidade === 1 ? 'pacote' : 'pacotes'} × {formatar.preco(pedido.precoUnitario)}
          {mostrarCliente && <> · para {pedido.clienteNome}</>}
        </p>
        <p className="pedido-endereco">{pedido.enderecoEntrega}</p>
      </div>
      <div className="pedido-lateral">
        <p className="pedido-total numero">{formatar.preco(pedido.total)}</p>
        <p className={`situacao situacao-${pedido.status}`}>{cancelado ? 'Cancelado' : 'Pendente'}</p>
        {!cancelado &&
          (confirmando ? (
            <div className="confirmar">
              <button className="botao botao-perigo" onClick={cancelar} disabled={enviando}>
                {enviando ? 'Cancelando…' : 'Confirmar cancelamento'}
              </button>
              <button className="link-botao" onClick={() => setConfirmando(false)}>Manter pedido</button>
            </div>
          ) : (
            <button className="botao botao-secundario" onClick={() => setConfirmando(true)}>Cancelar pedido</button>
          ))}
        {erro && <p className="mensagem mensagem-erro" role="alert">{erro.message}</p>}
      </div>
    </li>
  )
}
