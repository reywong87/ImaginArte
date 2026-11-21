import { useEffect, useState } from 'react'
import type { VotingCardStateResponse } from '../../models/roomState'
import { resolveCardAsset, type CardAssetResolution } from '../../services/cardAssets'
import './MobileVotingScreen.css'

interface MobileVotingScreenProps {
  clue: string | null
  votingCards: VotingCardStateResponse[]
  submittedVotingCardId: string | null
  selectedVotingCardId: string | null
  disabled: boolean
  statusMessage: string | null
  errorMessage: string | null
  onCheckStatus: (() => void) | null
  onRetryConnection: (() => void) | null
  onSelect: (votingCardId: string) => void
  onConfirm: () => void
}

function MobileVotingScreen({
  clue,
  votingCards,
  submittedVotingCardId,
  selectedVotingCardId,
  disabled,
  statusMessage,
  errorMessage,
  onCheckStatus,
  onRetryConnection,
  onSelect,
  onConfirm,
}: MobileVotingScreenProps) {
  const [assets, setAssets] = useState<Record<string, CardAssetResolution>>({})
  const cardIds = JSON.stringify(votingCards.map((card) => card.cardId))
  const orderedCards = [...votingCards].sort((first, second) => first.displayNumber - second.displayNumber)
  const selectedCard = orderedCards.find((card) => card.votingCardId === selectedVotingCardId)
  const canConfirm = !disabled && selectedCard !== undefined
    && selectedCard.votingCardId !== submittedVotingCardId
    && assets[selectedCard.cardId]?.isSelectable === true

  useEffect(() => {
    let active = true
    const ids: string[] = JSON.parse(cardIds)

    void Promise.all(ids.map(resolveCardAsset)).then((resolved) => {
      if (active) {
        setAssets(Object.fromEntries(resolved.map((asset) => [asset.cardId, asset])))
      }
    })

    return () => { active = false }
  }, [cardIds])

  return (
    <section className="mobile-voting" aria-labelledby="mobile-join-title">
      <header className="mobile-voting__header">
        <p>Tu turno</p>
        <h1 id="mobile-join-title" className="visually-hidden">Vota la carta del narrador</h1>
      </header>

      <div className="mobile-voting__clue">
        <h2>La pista</h2>
        <p>{clue?.trim() || 'Pista no disponible'}</p>
      </div>

      <fieldset className="mobile-voting__choices">
        <legend>Elige una carta</legend>
        <p className="mobile-voting__instruction">No puedes votar tu propia carta. Puedes cambiar tu elección antes de confirmar.</p>
        <div className="mobile-voting__cards" role="region" aria-label="Cartas de votación" tabIndex={0}>
          {orderedCards.map((card) => {
            const asset = assets[card.cardId]
            const ownCard = card.votingCardId === submittedVotingCardId
            const available = asset?.isSelectable === true && asset.imageUrl !== null
            const selected = card.votingCardId === selectedVotingCardId

            return (
              <label className={`mobile-voting__card${selected ? ' mobile-voting__card--selected' : ''}${ownCard ? ' mobile-voting__card--own' : ''}`} key={card.votingCardId}>
                <input
                  type="radio"
                  name="voting-card"
                  value={card.votingCardId}
                  checked={selected}
                  disabled={disabled || ownCard || !available}
                  onChange={() => onSelect(card.votingCardId)}
                  aria-label={`Carta ${card.displayNumber}: ${asset?.alt ?? 'Cargando ilustración'}${ownCard ? '. Tu carta, no puedes votarla' : ''}`}
                />
                <span className="mobile-voting__art">
                  {available ? (
                    <img
                      src={asset.imageUrl ?? undefined}
                      alt=""
                      onError={() => setAssets((current) => ({
                        ...current,
                        [card.cardId]: {
                          ...asset,
                          imageUrl: null,
                          isAvailable: false,
                          isSelectable: false,
                          availabilityMessage: 'Ilustración no disponible',
                        },
                      }))}
                    />
                  ) : <span>{asset?.availabilityMessage ?? 'Cargando ilustración…'}</span>}
                  {ownCard && <span className="mobile-voting__own-badge" aria-hidden="true">Tu carta</span>}
                </span>
                <span className="mobile-voting__number">Carta {card.displayNumber}</span>
                <span className="mobile-voting__label">
                  {ownCard ? 'No puedes votarla' : selected ? 'Seleccionada' : available ? 'Elegir carta' : asset ? 'No disponible' : 'Cargando'}
                </span>
              </label>
            )
          })}
        </div>
      </fieldset>

      <div className="mobile-voting__controls">
        {statusMessage ? <p role="status">{statusMessage}</p> : <p>Tu voto será privado.</p>}
        {errorMessage && <p role="alert">{errorMessage}</p>}
        {onCheckStatus && <button className="mobile-join__snapshot-retry" type="button" onClick={onCheckStatus}>Comprobar estado</button>}
        {onRetryConnection && <button className="mobile-join__snapshot-retry" type="button" onClick={onRetryConnection}>Reintentar conexión</button>}
        <button className="mobile-join__submit" type="button" disabled={!canConfirm} onClick={onConfirm}>
          {disabled ? 'Espera…' : 'Confirmar voto'}
        </button>
      </div>
    </section>
  )
}

export default MobileVotingScreen
