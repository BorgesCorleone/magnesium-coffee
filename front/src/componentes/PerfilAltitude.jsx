import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { formatar } from '../api.js'

// Perfil estilizado da Serra do Caparaó: x de 0 a 1000, altitude em metros.
const CRISTA = [
  [0, 640], [70, 760], [150, 880], [210, 860], [290, 1080], [360, 1180], [420, 1150],
  [500, 1520], [560, 1900], [610, 2380], [650, 2892], [690, 2560], [740, 2150],
  [800, 1680], [860, 1320], [910, 1120], [960, 920], [1000, 780],
]
const PICO_X = 650
const ALT_MIN = 500
const ALT_MAX = 3000
const LARGURA = 1000
const ALTURA = 360

const y = (alt) => ALTURA - ((alt - ALT_MIN) / (ALT_MAX - ALT_MIN)) * ALTURA

// Encontra o x da encosta (esquerda ou direita do pico) onde a crista atinge a altitude.
function xNaEncosta(alt, lado) {
  const pontos = lado === 'esquerda' ? CRISTA.filter(([x]) => x <= PICO_X) : [...CRISTA.filter(([x]) => x >= PICO_X)].reverse()
  for (let i = 0; i < pontos.length - 1; i++) {
    const [x1, a1] = pontos[i]
    const [x2, a2] = pontos[i + 1]
    if (alt >= Math.min(a1, a2) && alt <= Math.max(a1, a2) && a1 !== a2) return x1 + ((alt - a1) / (a2 - a1)) * (x2 - x1)
  }
  return lado === 'esquerda' ? 20 : 980
}

const caminhoCrista = 'M' + CRISTA.map(([x, a]) => `${x},${y(a).toFixed(1)}`).join(' L')
const caminhoArea = `${caminhoCrista} L${LARGURA},${ALTURA} L0,${ALTURA} Z`

export default function PerfilAltitude({ cafes }) {
  const navegar = useNavigate()
  const [ativo, setAtivo] = useState(null)

  const pontos = useMemo(
    () =>
      [...cafes]
        .sort((a, b) => a.altitudeMetros - b.altitudeMetros)
        .map((cafe, i) => {
          const lado = i % 2 === 0 ? 'esquerda' : 'direita'
          return { cafe, lado, x: xNaEncosta(cafe.altitudeMetros, lado), y: y(cafe.altitudeMetros), ordem: i }
        }),
    [cafes],
  )

  const destaque = pontos.find((p) => p.cafe.id === ativo)

  return (
    <figure className="perfil">
      <svg viewBox={`0 -40 ${LARGURA} ${ALTURA + 40}`} className="perfil-svg" role="group" aria-label="Cafés no perfil de altitude da Serra do Caparaó">
        {[1000, 1500, 2000, 2500].map((alt) => (
          <g key={alt} className="perfil-curva">
            <line x1="0" x2={LARGURA} y1={y(alt)} y2={y(alt)} />
            <text x="6" y={y(alt) - 6}>{formatar.altitude(alt)}</text>
          </g>
        ))}

        <path d={caminhoArea} className="perfil-area" />
        <path d={caminhoCrista} className="perfil-crista" pathLength="1" />

        <g className="perfil-pico">
          <circle cx={PICO_X} cy={y(2892)} r="3.5" />
          <text x={PICO_X + 12} y={y(2892) + 4}>Pico da Bandeira, 2.892 m</text>
        </g>

        {pontos.map(({ cafe, x, y: py, ordem }) => (
          <g
            key={cafe.id}
            className={`perfil-ponto${ativo === cafe.id ? ' perfil-ponto-ativo' : ''}${cafe.disponivel ? '' : ' perfil-ponto-esgotado'}`}
            style={{ '--atraso': `${0.9 + ordem * 0.08}s` }}
            transform={`translate(${x.toFixed(1)} ${py.toFixed(1)})`}
            tabIndex={0}
            role="link"
            aria-label={`${cafe.nome}, ${formatar.altitude(cafe.altitudeMetros)}, ${cafe.municipio}`}
            onMouseEnter={() => setAtivo(cafe.id)}
            onMouseLeave={() => setAtivo(null)}
            onFocus={() => setAtivo(cafe.id)}
            onBlur={() => setAtivo(null)}
            onClick={() => navegar(`/cafes/${cafe.id}`)}
            onKeyDown={(e) => (e.key === 'Enter' || e.key === ' ') && (e.preventDefault(), navegar(`/cafes/${cafe.id}`))}
          >
            <circle r="16" className="perfil-ponto-alvo" />
            <circle r="7" className="perfil-ponto-cereja" />
          </g>
        ))}

        {destaque && (
          <g
            className="perfil-rotulo"
            transform={`translate(${Math.min(Math.max(destaque.x, 130), LARGURA - 130).toFixed(1)} ${(destaque.y - 30).toFixed(1)})`}
            aria-hidden="true"
          >
            <rect x="-125" y="-30" width="250" height="40" rx="8" />
            <text y="-14" textAnchor="middle" className="perfil-rotulo-nome">{destaque.cafe.nome}</text>
            <text y="2" textAnchor="middle">
              {formatar.altitude(destaque.cafe.altitudeMetros)} · {destaque.cafe.municipio}
            </text>
          </g>
        )}
      </svg>
      <figcaption>
        Cada ponto vermelho é um café do catálogo, na altitude em que foi cultivado. Toque ou passe o mouse para ver; clique para abrir.
      </figcaption>
    </figure>
  )
}
