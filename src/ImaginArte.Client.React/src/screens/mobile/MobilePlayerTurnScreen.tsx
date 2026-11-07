import { useEffect, useState } from 'react'
import { resolveCardAsset, type CardAssetResolution } from '../../services/cardAssets'
import './MobilePlayerTurnScreen.css'

const exampleCardIds = ['TEMP-001', 'TEMP-002', 'TEMP-003', 'TEMP-004', 'TEMP-005', 'TEMP-006']

function MobilePlayerTurnScreen() {
  const [assets, setAssets] = useState<Record<string, CardAssetResolution>>({})
  const [selectedCardId, setSelectedCardId] = useState<string | null>(null)
  const [showUnavailableNotice, setShowUnavailableNotice] = useState(false)
  const canSubmit = selectedCardId !== null && assets[selectedCardId]?.isSelectable === true

  useEffect(() => {
    let isCurrent = true

    void Promise.all(exampleCardIds.map(resolveCardAsset)).then((resolvedAssets) => {
      if (isCurrent) {
        setAssets(Object.fromEntries(resolvedAssets.map((asset) => [asset.cardId, asset])))
      }
    })

    return () => { isCurrent = false }
  }, [])

  return (
    <section className="player-turn" aria-labelledby="mobile-join-title">
      <header className="player-turn__header">
        <p>Maqueta de desarrollo · Datos de ejemplo</p>
        <h1 id="mobile-join-title">Elige una carta para la pista</h1>
      </header>

      <div className="player-turn__clue">
        <h2>Pista de ejemplo</h2>
        <p>Donde termina la noche</p>
      </div>

      <fieldset className="player-turn__hand">
        <legend>Mano de ejemplo</legend>
        <p className="player-turn__instruction">Elige una carta de ejemplo. Puedes cambiarla antes de continuar.</p>
        <div className="player-turn__cards" tabIndex={0} role="region" aria-label="Cartas de ejemplo">
          {exampleCardIds.map((cardId, index) => {
            const asset = assets[cardId]
            const available = asset?.isSelectable === true && asset.imageUrl !== null
            const selected = selectedCardId === cardId && available

            return (
              <label className={`player-turn__card${selected ? ' player-turn__card--selected' : ''}`} key={cardId}>
                <input
                  type="radio"
                  name="player-example-card"
                  value={cardId}
                  checked={selected}
                  disabled={!available}
                  onChange={() => {
                    setSelectedCardId(cardId)
                    setShowUnavailableNotice(false)
                  }}
                  aria-label={`Carta de ejemplo ${index + 1}: ${asset?.alt ?? 'Cargando ilustración'}`}
                />
                <span className="player-turn__art">
                  {available ? (
                    <img
                      src={asset.imageUrl ?? undefined}
                      alt=""
                      onError={() => setAssets((current) => ({
                        ...current,
                        [cardId]: {
                          ...asset,
                          imageUrl: null,
                          isAvailable: false,
                          isSelectable: false,
                          availabilityMessage: 'Ilustración no disponible',
                        },
                      }))}
                    />
                  ) : (
                    <span>{asset?.availabilityMessage ?? 'Cargando ilustración…'}</span>
                  )}
                </span>
                <span>Carta de ejemplo {index + 1} · {selected ? 'Seleccionada' : available ? 'Elegir carta' : asset ? 'No disponible' : 'Cargando'}</span>
              </label>
            )
          })}
        </div>
      </fieldset>

      <div className="player-turn__controls">
        {showUnavailableNotice && <p role="status">El envío de cartas aún no está disponible en esta maqueta.</p>}
        <button className="mobile-join__submit" type="button" disabled={!canSubmit} onClick={() => setShowUnavailableNotice(true)}>
          Enviar carta
        </button>
      </div>
    </section>
  )
}

export default MobilePlayerTurnScreen
