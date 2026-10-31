import { TORRAS } from '../api.js'

const NIVEL = { clara: 1, media: 2, escura: 3 }

// Três grãos que escurecem conforme a torra.
export default function Torra({ torra, comTexto = true }) {
  const nivel = NIVEL[torra] ?? 0
  return (
    <span className="torra" title={`Torra ${TORRAS[torra] ?? torra}`}>
      <span className="torra-graos" aria-hidden="true">
        {[1, 2, 3].map((n) => (
          <svg key={n} viewBox="0 0 10 14" className={n <= nivel ? `grao grao-${torra}` : 'grao grao-vazio'}>
            <ellipse cx="5" cy="7" rx="4.2" ry="6.2" />
            <path d="M5 1.4 C3.4 4.5 6.6 9.5 5 12.6" />
          </svg>
        ))}
      </span>
      {comTexto && <span>Torra {TORRAS[torra]?.toLowerCase() ?? torra}</span>}
    </span>
  )
}
