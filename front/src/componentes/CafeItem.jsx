import { Link } from 'react-router-dom'
import { formatar } from '../api.js'
import Torra from './Torra.jsx'

const ALT_MAX = 1500

export default function CafeItem({ cafe }) {
  const proporcao = Math.min(cafe.altitudeMetros / ALT_MAX, 1)
  return (
    <article className={`cafe-item${cafe.disponivel ? '' : ' cafe-item-esgotado'}`}>
      <div className="cafe-item-altitude" aria-hidden="true">
        <span style={{ '--altura': proporcao }} />
      </div>
      <div className="cafe-item-corpo">
        <p className="cafe-item-local">
          {cafe.municipio}, {formatar.altitude(cafe.altitudeMetros)}
        </p>
        <h3>
          <Link to={`/cafes/${cafe.id}`} className="cafe-item-link">
            {cafe.nome}
          </Link>
        </h3>
        <p className="cafe-item-produtor">{cafe.produtor}</p>
        <dl className="cafe-item-dados">
          <div>
            <dt>Variedade</dt>
            <dd>{cafe.variedade}</dd>
          </div>
          <div>
            <dt>Nota SCA</dt>
            <dd className="numero">{formatar.nota(cafe.pontuacaoSca)}</dd>
          </div>
        </dl>
        <Torra torra={cafe.torra} />
        <div className="cafe-item-rodape">
          <p className="cafe-item-preco">
            <strong className="numero">{formatar.preco(cafe.preco)}</strong>
            <span>{formatar.peso(cafe.pesoGramas)}</span>
          </p>
          <p className={cafe.disponivel ? 'estoque' : 'estoque estoque-zero'}>
            {cafe.disponivel ? `${cafe.estoque} em estoque` : 'Esgotado'}
          </p>
        </div>
      </div>
    </article>
  )
}
