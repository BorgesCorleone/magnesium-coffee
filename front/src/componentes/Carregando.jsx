import { useEffect, useState } from 'react'

// A instância gratuita da Render dorme sem uso; a primeira resposta pode levar até um minuto.
export default function Carregando({ texto = 'Carregando…' }) {
  const [demorando, setDemorando] = useState(false)

  useEffect(() => {
    const t = setTimeout(() => setDemorando(true), 4000)
    return () => clearTimeout(t)
  }, [])

  return (
    <div className="carregando" role="status">
      <span className="carregando-grao" aria-hidden="true" />
      <p>{texto}</p>
      {demorando && (
        <p className="carregando-dica">
          O servidor estava em repouso e está acordando. Na primeira visita isso pode levar até um minuto.
        </p>
      )}
    </div>
  )
}

export function Falha({ erro, aoTentarDeNovo }) {
  return (
    <div className="falha" role="alert">
      <p>{erro.message}</p>
      {aoTentarDeNovo && (
        <button className="botao botao-secundario" onClick={aoTentarDeNovo}>
          Tentar de novo
        </button>
      )}
    </div>
  )
}
