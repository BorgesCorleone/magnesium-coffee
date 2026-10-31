export default function Paginacao({ pagina, totalPaginas, aoMudar }) {
  if (totalPaginas <= 1) return null
  return (
    <nav className="paginacao" aria-label="Paginação">
      <button className="botao botao-secundario" disabled={pagina <= 1} onClick={() => aoMudar(pagina - 1)}>
        Anterior
      </button>
      <span>
        Página {pagina} de {totalPaginas}
      </span>
      <button className="botao botao-secundario" disabled={pagina >= totalPaginas} onClick={() => aoMudar(pagina + 1)}>
        Próxima
      </button>
    </nav>
  )
}
