import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api, formatar, TORRAS } from '../api.js'
import { useSessao } from '../sessao.jsx'
import { useAviso } from '../componentes/Aviso.jsx'
import useCarregar from '../useCarregar.js'
import Carregando, { Falha } from '../componentes/Carregando.jsx'
import Campo from '../componentes/Campo.jsx'
import Paginacao from '../componentes/Paginacao.jsx'

export function AdminCafes() {
  const [pagina, setPagina] = useState(1)
  const lista = useCarregar(() => api(`/api/cafes?pagina=${pagina}&tamanho=15`), [pagina])

  return (
    <section className="pagina">
      <div className="secao-topo">
        <h1>Gerenciar cafés</h1>
        <Link to="/admin/cafes/novo" className="botao botao-primario">Cadastrar café</Link>
      </div>
      {lista.carregando && !lista.dados && <Carregando />}
      {lista.erro && <Falha erro={lista.erro} aoTentarDeNovo={lista.recarregar} />}
      {lista.dados && (
        <>
          <div className="tabela-rolagem">
            <table className="tabela">
              <thead>
                <tr>
                  <th scope="col">Café</th>
                  <th scope="col">Município</th>
                  <th scope="col">Torra</th>
                  <th scope="col" className="numero">Preço</th>
                  <th scope="col" className="numero">Estoque</th>
                  <th scope="col"><span className="visualmente-oculto">Ações</span></th>
                </tr>
              </thead>
              <tbody>
                {lista.dados.itens.map((cafe) => (
                  <LinhaCafe key={cafe.id} cafe={cafe} aoMudar={lista.recarregar} />
                ))}
              </tbody>
            </table>
          </div>
          <Paginacao pagina={lista.dados.pagina} totalPaginas={lista.dados.totalPaginas} aoMudar={setPagina} />
        </>
      )}
    </section>
  )
}

function LinhaCafe({ cafe, aoMudar }) {
  const { chamar } = useSessao()
  const avisar = useAviso()
  const [confirmando, setConfirmando] = useState(false)
  const [erro, setErro] = useState(null)

  async function excluir() {
    setErro(null)
    try {
      await chamar(`/api/cafes/${cafe.id}`, { metodo: 'DELETE' })
      avisar(`“${cafe.nome}” excluído do catálogo.`)
      aoMudar()
    } catch (falha) {
      setErro(falha)
      setConfirmando(false)
    }
  }

  return (
    <>
      <tr>
        <td>
          <Link to={`/cafes/${cafe.id}`} className="tabela-nome">{cafe.nome}</Link>
          <span className="tabela-sub">{cafe.produtor}</span>
        </td>
        <td>{cafe.municipio}</td>
        <td>{TORRAS[cafe.torra]}</td>
        <td className="numero">{formatar.preco(cafe.preco)}</td>
        <td className={`numero${cafe.estoque === 0 ? ' estoque-zero' : ''}`}>{cafe.estoque}</td>
        <td className="tabela-acoes">
          {confirmando ? (
            <>
              <button className="botao botao-perigo botao-pequeno" onClick={excluir}>Confirmar exclusão</button>
              <button className="link-botao" onClick={() => setConfirmando(false)}>Manter</button>
            </>
          ) : (
            <>
              <Link to={`/admin/cafes/${cafe.id}`} className="botao botao-secundario botao-pequeno">Editar</Link>
              <button className="botao botao-fantasma botao-pequeno" onClick={() => setConfirmando(true)}>Excluir</button>
            </>
          )}
        </td>
      </tr>
      {erro && (
        <tr className="tabela-erro">
          <td colSpan={6}>
            <p className="mensagem mensagem-conflito" role="alert">{erro.message}</p>
          </td>
        </tr>
      )}
    </>
  )
}

const VAZIO = {
  nome: '', produtor: '', municipio: '', variedade: '', processo: '', torra: 'media',
  altitudeMetros: 1100, pontuacaoSca: 84, pesoGramas: 250, preco: '', estoque: 0,
  dataTorra: new Date().toISOString().slice(0, 10), descricao: '',
}

const MUNICIPIOS = ['Alto Caparaó', 'Divino de São Lourenço', 'Dores do Rio Preto', 'Espera Feliz', 'Guaçuí', 'Ibitirama', 'Irupi', 'Iúna']

export function AdminCafeFormulario() {
  const { id } = useParams()
  const editando = id !== undefined
  const existente = useCarregar(() => (editando ? api(`/api/cafes/${id}`) : Promise.resolve(null)), [id])

  if (existente.carregando) return <section className="pagina"><Carregando /></section>
  if (existente.erro) return <section className="pagina pagina-estreita"><Falha erro={existente.erro} /></section>

  const inicial = existente.dados ? { ...existente.dados, dataTorra: existente.dados.dataTorra.slice(0, 10) } : VAZIO
  return <FormularioCafe key={id ?? 'novo'} inicial={inicial} id={id} />
}

function FormularioCafe({ inicial, id }) {
  const { chamar } = useSessao()
  const avisar = useAviso()
  const navegar = useNavigate()
  const [cafe, setCafe] = useState(inicial)
  const [enviando, setEnviando] = useState(false)
  const [erro, setErro] = useState(null)

  const campo = (nome) => ({
    value: cafe[nome],
    onChange: (e) => setCafe((c) => ({ ...c, [nome]: e.target.value })),
    erro: erro?.campos?.[nome],
  })

  async function salvar(e) {
    e.preventDefault()
    setEnviando(true)
    setErro(null)
    const corpo = {
      ...cafe,
      altitudeMetros: Number(cafe.altitudeMetros),
      pontuacaoSca: Number(cafe.pontuacaoSca),
      pesoGramas: Number(cafe.pesoGramas),
      preco: Number(String(cafe.preco).replace(',', '.')),
      estoque: Number(cafe.estoque),
      // meio-dia em Brasília, para a data não "voltar um dia" ao converter para UTC
      dataTorra: cafe.dataTorra ? `${cafe.dataTorra}T12:00:00-03:00` : null,
    }
    try {
      const salvo = await chamar(id ? `/api/cafes/${id}` : '/api/cafes', { metodo: id ? 'PUT' : 'POST', corpo })
      avisar(id ? `“${salvo.nome}” atualizado.` : `“${salvo.nome}” cadastrado no catálogo.`)
      navegar('/admin')
    } catch (falha) {
      setErro(falha)
    } finally {
      setEnviando(false)
    }
  }

  return (
    <section className="pagina pagina-media">
      <Link to="/admin" className="voltar">Voltar para a lista</Link>
      <h1>{id ? `Editar ${inicial.nome}` : 'Cadastrar café'}</h1>
      <form className="formulario-cafe" onSubmit={salvar} noValidate>
        <fieldset>
          <legend>Origem</legend>
          <Campo rotulo="Nome do café" {...campo('nome')} />
          <Campo rotulo="Produtor" {...campo('produtor')} dica="Sítio ou fazenda e a família responsável." />
          <Campo rotulo="Município" list="municipios" {...campo('municipio')} />
          <datalist id="municipios">{MUNICIPIOS.map((m) => <option key={m} value={m} />)}</datalist>
          <Campo rotulo="Altitude (m)" type="number" min="0" max="3000" {...campo('altitudeMetros')} />
        </fieldset>
        <fieldset>
          <legend>Grão e torra</legend>
          <Campo rotulo="Variedade" {...campo('variedade')} />
          <Campo rotulo="Processo" {...campo('processo')} dica="Natural, lavado, cereja descascado, fermentado…" />
          <Campo rotulo="Torra" como="select" opcoes={Object.entries(TORRAS)} {...campo('torra')} />
          <Campo rotulo="Nota SCA" type="number" step="0.5" min="0" max="100" {...campo('pontuacaoSca')} />
          <Campo rotulo="Data da torra" type="date" {...campo('dataTorra')} />
        </fieldset>
        <fieldset>
          <legend>Venda</legend>
          <Campo rotulo="Preço (R$)" inputMode="decimal" {...campo('preco')} />
          <Campo rotulo="Peso do pacote (g)" type="number" min="50" max="5000" {...campo('pesoGramas')} />
          <Campo rotulo="Estoque (pacotes)" type="number" min="0" {...campo('estoque')} />
        </fieldset>
        <Campo rotulo="Descrição" como="textarea" rows={3} {...campo('descricao')} dica="Notas sensoriais, até 500 caracteres." />
        {erro && !Object.keys(erro.campos).length && <p className="mensagem mensagem-erro" role="alert">{erro.message}</p>}
        <div className="formulario-acoes">
          <button className="botao botao-primario" disabled={enviando}>{enviando ? 'Salvando…' : id ? 'Salvar alterações' : 'Cadastrar café'}</button>
          <Link to="/admin" className="botao botao-fantasma">Descartar</Link>
        </div>
      </form>
    </section>
  )
}
