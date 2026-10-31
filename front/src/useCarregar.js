import { useCallback, useEffect, useState } from 'react'

// Carrega dados de uma função assíncrona e expõe { dados, erro, carregando, recarregar }.
export default function useCarregar(buscar, dependencias) {
  const [estado, setEstado] = useState({ dados: null, erro: null, carregando: true })
  const [versao, setVersao] = useState(0)

  useEffect(() => {
    let ativo = true
    setEstado((e) => ({ ...e, erro: null, carregando: true }))
    buscar()
      .then((dados) => ativo && setEstado({ dados, erro: null, carregando: false }))
      .catch((erro) => ativo && setEstado({ dados: null, erro, carregando: false }))
    return () => {
      ativo = false
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [...dependencias, versao])

  const recarregar = useCallback(() => setVersao((v) => v + 1), [])
  return { ...estado, recarregar }
}
