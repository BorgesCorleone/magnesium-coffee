import { useId } from 'react'

// Rótulo + controle + mensagem de erro do campo vinda da API (400).
export default function Campo({ rotulo, erro, dica, children, opcoes, como = 'input', ...props }) {
  const id = useId()
  const Controle = como
  const descricao = erro ? `${id}-erro` : dica ? `${id}-dica` : undefined
  return (
    <div className={`campo${erro ? ' campo-com-erro' : ''}`}>
      <label htmlFor={id}>{rotulo}</label>
      {children ?? (
        <Controle id={id} aria-invalid={!!erro} aria-describedby={descricao} {...props}>
          {opcoes?.map(([valor, texto]) => (
            <option key={valor} value={valor}>{texto}</option>
          ))}
        </Controle>
      )}
      {erro ? (
        <p id={`${id}-erro`} className="campo-erro">
          {erro}
        </p>
      ) : (
        dica && (
          <p id={`${id}-dica`} className="campo-dica">
            {dica}
          </p>
        )
      )}
    </div>
  )
}
