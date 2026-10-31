import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api, formatar, TORRAS } from '../api.js'
import { useSessao } from '../sessao.jsx'
import { useAviso } from '../componentes/Aviso.jsx'
import useCarregar from '../useCarregar.js'
import Carregando, { Falha } from '../componentes/Carregando.jsx'
import Campo from '../componentes/Campo.jsx'
import Torra from '../componentes/Torra.jsx'

export default function CafeDetalhe() {
  const { id } = useParams()
  const { dados: cafe, erro, carregando, recarregar } = useCarregar(() => api(`/api/cafes/${id}`), [id])

  if (carregando) return <section className="pagina"><Carregando texto="Abrindo o café…" /></section>
  if (erro)
    return (
      <section className="pagina pagina-estreita">
        <h1>{erro.status === 404 ? 'Café não encontrado' : 'Não foi possível abrir este café'}</h1>
        <Falha erro={erro} aoTentarDeNovo={erro.status === 404 ? null : recarregar} />
        <Link to="/" className="botao botao-secundario">Ver todos os cafés</Link>
      </section>
    )

  return (
    <section className="pagina detalhe">
      <Link to="/" className="voltar">Voltar ao catálogo</Link>
      <div className="detalhe-grade">
        <div className="detalhe-info">
          <p className="cafe-item-local">{cafe.municipio}, Serra do Caparaó</p>
          <h1>{cafe.nome}</h1>
          <p className="detalhe-produtor">{cafe.produtor}</p>
          {cafe.descricao && <p className="detalhe-descricao">{cafe.descricao}</p>}

          <dl className="ficha">
            <div><dt>Altitude</dt><dd className="numero">{formatar.altitude(cafe.altitudeMetros)}</dd></div>
            <div><dt>Variedade</dt><dd>{cafe.variedade}</dd></div>
            <div><dt>Processo</dt><dd>{cafe.processo}</dd></div>
            <div><dt>Torra</dt><dd><Torra torra={cafe.torra} comTexto={false} /> {TORRAS[cafe.torra]}</dd></div>
            <div><dt>Nota SCA</dt><dd className="numero">{formatar.nota(cafe.pontuacaoSca)} pontos</dd></div>
            <div><dt>Data da torra</dt><dd>{formatar.data(cafe.dataTorra)}</dd></div>
            <div><dt>Pacote</dt><dd>{formatar.peso(cafe.pesoGramas)}</dd></div>
          </dl>
        </div>

        <CaixaPedido cafe={cafe} aoPedir={recarregar} />
      </div>
    </section>
  )
}

function CaixaPedido({ cafe, aoPedir }) {
  const { logado, chamar } = useSessao()
  const avisar = useAviso()
  const navegar = useNavigate()
  const [quantidade, setQuantidade] = useState(1)
  const [endereco, setEndereco] = useState('')
  const [enviando, setEnviando] = useState(false)
  const [erro, setErro] = useState(null)

  const total = cafe.preco * (Number(quantidade) || 0)

  async function pedir(e) {
    e.preventDefault()
    setEnviando(true)
    setErro(null)
    try {
      const pedido = await chamar('/api/pedidos', {
        metodo: 'POST',
        corpo: { cafeId: cafe.id, quantidade: Number(quantidade), enderecoEntrega: endereco },
      })
      avisar(`Pedido nº ${pedido.id} feito: ${pedido.quantidade} × ${cafe.nome}.`)
      aoPedir()
      navegar('/pedidos')
    } catch (falha) {
      setErro(falha)
      if (falha.status === 409) aoPedir() // atualiza o estoque mostrado
    } finally {
      setEnviando(false)
    }
  }

  return (
    <aside className="caixa-pedido" aria-labelledby="titulo-pedido">
      <p className="caixa-preco">
        <strong className="numero">{formatar.preco(cafe.preco)}</strong> o pacote de {formatar.peso(cafe.pesoGramas)}
      </p>
      <p className={cafe.disponivel ? 'estoque' : 'estoque estoque-zero'}>
        {cafe.disponivel ? `${cafe.estoque} pacotes em estoque` : 'Esgotado no momento'}
      </p>

      <h2 id="titulo-pedido" className="visualmente-oculto">Fazer pedido</h2>
      {!logado ? (
        <div className="caixa-entrar">
          <p>Entre na sua conta para pedir este café.</p>
          <Link to="/entrar" state={{ voltarPara: `/cafes/${cafe.id}` }} className="botao botao-primario">Entrar para pedir</Link>
        </div>
      ) : (
        <form onSubmit={pedir} noValidate>
          <Campo rotulo="Quantidade de pacotes" erro={erro?.campos?.quantidade}>
            <div className="contador">
              <button type="button" aria-label="Diminuir" onClick={() => setQuantidade((q) => Math.max(1, Number(q) - 1))}>−</button>
              <input
                type="number"
                min="1"
                max="50"
                value={quantidade}
                onChange={(e) => setQuantidade(e.target.value)}
                aria-label="Quantidade de pacotes"
                className="numero"
              />
              <button type="button" aria-label="Aumentar" onClick={() => setQuantidade((q) => Math.min(50, Number(q) + 1))}>+</button>
            </div>
          </Campo>
          <Campo
            rotulo="Endereço de entrega"
            como="textarea"
            rows={3}
            value={endereco}
            onChange={(e) => setEndereco(e.target.value)}
            placeholder="Rua, número, bairro, cidade/UF"
            erro={erro?.campos?.enderecoEntrega}
          />
          <p className="caixa-total">
            Total <strong className="numero">{formatar.preco(total)}</strong>
          </p>
          {erro && !Object.keys(erro.campos).length && (
            <p className={`mensagem ${erro.status === 409 ? 'mensagem-conflito' : 'mensagem-erro'}`} role="alert">
              {erro.message}
            </p>
          )}
          <button className="botao botao-primario botao-largo" disabled={enviando}>
            {enviando ? 'Enviando pedido…' : 'Fazer pedido'}
          </button>
        </form>
      )}
    </aside>
  )
}
