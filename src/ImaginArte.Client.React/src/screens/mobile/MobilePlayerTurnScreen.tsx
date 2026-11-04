import { useEffect, useState } from 'react'
import { resolveCardAsset, type CardAssetResolution } from '../../services/cardAssets'
import './MobilePlayerTurnScreen.css'

const exampleCardIds = ['TEMP-001', 'TEMP-002', 'TEMP-003', 'TEMP-004', 'TEMP-005', 'TEMP-006']

function MobilePlayerTurnScreen() {
  const [assets, setAssets] = useState<Record<string, CardAssetResolution>>({})

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
    <section className="player-turn" aria-labelledby="player-turn-title">
      <header className="player-turn__header">
        <p>Maqueta de desarrollo · Datos de ejemplo</p>
        <h1 id="player-turn-title">Elige una carta para la pista</h1>
      </header>

      <div className="player-turn__clue">
        <h2>Pista de ejemplo</h2>
        <p>Donde termina la noche</p>
      </div>

      <section className="player-turn__hand" aria-labelledby="player-turn-hand-title">
        <h2 id="player-turn-hand-title">Mano de ejemplo</h2>
        <div className="player-turn__cards" tabIndex={0} role="region" aria-label="Cartas de ejemplo">
          {exampleCardIds.map((cardId, index) => {
            const asset = assets[cardId]
            const available = asset?.isSelectable === true && asset.imageUrl !== null

            return (
              <div className="player-turn__card" key={cardId}>
                <div className="player-turn__art">
                  {available ? (
                    <img
                      src={asset.imageUrl ?? undefined}
                      alt={`Carta de ejemplo ${index + 1}: ${asset.alt}`}
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
                </div>
                <span>Carta de ejemplo {index + 1}</span>
              </div>
            )
          })}
        </div>
      </section>
    </section>
  )
}

export default MobilePlayerTurnScreen
