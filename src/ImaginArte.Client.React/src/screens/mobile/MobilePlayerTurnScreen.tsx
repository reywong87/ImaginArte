import { useEffect, useState } from 'react'
import type { PrivateCardStateResponse } from '../../models/roomState'
import { resolveCardAsset, type CardAssetResolution } from '../../services/cardAssets'
import './MobilePlayerTurnScreen.css'

interface MobilePlayerTurnScreenProps {
  clue: string | null
  hand: PrivateCardStateResponse[]
}

function MobilePlayerTurnScreen({ clue, hand }: MobilePlayerTurnScreenProps) {
  const [assets, setAssets] = useState<Record<string, CardAssetResolution>>({})
  const [selectedCardId, setSelectedCardId] = useState<string | null>(null)
  const [showUnavailableNotice, setShowUnavailableNotice] = useState(false)
  const handIds = JSON.stringify(hand.map((card) => card.cardId))
  const canSubmit = Boolean(clue?.trim()) && selectedCardId !== null
    && hand.some((card) => card.cardId === selectedCardId)
    && assets[selectedCardId]?.isSelectable === true

  useEffect(() => {
    let isCurrent = true

    const cardIds: string[] = JSON.parse(handIds)
    void Promise.all(cardIds.map(resolveCardAsset)).then((resolvedAssets) => {
      if (isCurrent) {
        setAssets(Object.fromEntries(resolvedAssets.map((asset) => [asset.cardId, asset])))
      }
    })

    return () => { isCurrent = false }
  }, [handIds])

  return (
    <section className="player-turn" aria-labelledby="mobile-join-title">
      <header className="player-turn__header">
        <p>Tu turno</p>
        <h1 id="mobile-join-title">Elige una carta para la pista</h1>
      </header>

      <div className="player-turn__clue">
        <h2>Pista</h2>
        <p role={!clue?.trim() ? 'status' : undefined}>{clue?.trim() ? clue : 'La pista no está disponible. Espera una actualización de la sala.'}</p>
      </div>

      <fieldset className="player-turn__hand">
        <legend>Tu mano privada</legend>
        <p className="player-turn__instruction">Elige una carta. Puedes cambiarla antes de continuar.</p>
        <div className="player-turn__cards" tabIndex={0} role="region" aria-label="Cartas de tu mano">
          {hand.map(({ cardId }, index) => {
            const asset = assets[cardId]
            const available = asset?.isSelectable === true && asset.imageUrl !== null
            const selected = selectedCardId === cardId && available

            return (
              <label className={`player-turn__card${selected ? ' player-turn__card--selected' : ''}`} key={cardId}>
                <input
                  type="radio"
                  name="player-card"
                  value={cardId}
                  checked={selected}
                  disabled={!available}
                  onChange={() => {
                    setSelectedCardId(cardId)
                    setShowUnavailableNotice(false)
                  }}
                  aria-label={`Carta ${index + 1}: ${asset?.alt ?? 'Cargando ilustración'}`}
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
                <span>Carta {index + 1} · {selected ? 'Seleccionada' : available ? 'Elegir carta' : asset ? 'No disponible' : 'Cargando'}</span>
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
