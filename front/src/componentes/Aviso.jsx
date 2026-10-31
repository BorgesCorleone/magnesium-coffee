import { createContext, useCallback, useContext, useRef, useState } from 'react'

const AvisoContexto = createContext(() => {})

// Mensagem curta de confirmação ("Pedido feito"), some sozinha.
export function AvisoProvider({ children }) {
  const [aviso, setAviso] = useState(null)
  const timer = useRef()

  const avisar = useCallback((texto, tipo = 'ok') => {
    clearTimeout(timer.current)
    setAviso({ texto, tipo, id: Date.now() })
    timer.current = setTimeout(() => setAviso(null), 4200)
  }, [])

  return (
    <AvisoContexto.Provider value={avisar}>
      {children}
      <div className="aviso-area" role="status" aria-live="polite">
        {aviso && (
          <p key={aviso.id} className={`aviso aviso-${aviso.tipo}`}>
            {aviso.texto}
          </p>
        )}
      </div>
    </AvisoContexto.Provider>
  )
}

export const useAviso = () => useContext(AvisoContexto)
