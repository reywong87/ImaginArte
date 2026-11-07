import { useEffect, useRef, useState } from 'react'
import type { PrivateCardStateResponse } from '../../models/roomState'
import { resolveCardAsset, type CardAssetResolution } from '../../services/cardAssets'
import { RoomApiError, submitGuessCard } from '../../services/roomsApi'
import './MobilePlayerTurnScreen.css'

interface MobilePlayerTurnScreenProps {
  clue: string | null
  hand: PrivateCardStateResponse[]
  roomCode: string
  playerId: string
  connected: boolean
}

function MobilePlayerTurnScreen({ clue, hand, roomCode, playerId, connected }: MobilePlayerTurnScreenProps) {
  const [assets, setAssets] = useState<Record<string, CardAssetResolution>>({})
  const [selectedCardId, setSelectedCardId] = useState<string | null>(null)
  const [submissionStatus, setSubmissionStatus] = useState<'idle' | 'submitting' | 'uncertain' | 'confirmed'>('idle')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const inFlight = useRef(false)
  const mounted = useRef(true)
  const handIds = JSON.stringify(hand.map((card) => card.cardId))
  const canSubmit = connected && submissionStatus === 'idle' && Boolean(clue?.trim()) && selectedCardId !== null
    && hand.some((card) => card.cardId === selectedCardId)
    && assets[selectedCardId]?.isSelectable === true

  useEffect(() => {
    mounted.current = true
    return () => { mounted.current = false }
  }, [])

  async function handleSubmit() {
    if (!canSubmit || selectedCardId === null || inFlight.current) return
    inFlight.current = true
    setSubmissionStatus('submitting')
    setErrorMessage(null)
    try {
      await submitGuessCard(roomCode, playerId, selectedCardId)
      if (mounted.current) setSubmissionStatus('confirmed')
    } catch (error) {
      if (mounted.current) {
        if (error instanceof RoomApiError && error.status === 400) {
          setSubmissionStatus('idle')
          setErrorMessage('El servidor rechazó el envío. Conservamos tu selección.')
        } else {
          setSubmissionStatus('uncertain')
          setErrorMessage('No se pudo confirmar el envío. Es necesario comprobar el estado antes de volver a enviar.')
        }
      }
    } finally {
      inFlight.current = false
    }
  }

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

  if (submissionStatus === 'confirmed') {
    return <section role="status"><h1 id="mobile-join-title">Carta enviada</h1><p>Esperando la actualización de la sala…</p></section>
  }

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
                  disabled={!available || submissionStatus !== 'idle'}
                  onChange={() => {
                    setSelectedCardId(cardId)
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
        {!connected && <p role="status">Esperando la conexión con la sala…</p>}
        {submissionStatus === 'submitting' && <p role="status">Enviando carta…</p>}
        {errorMessage && <p role="alert">{errorMessage}</p>}
        <button className="mobile-join__submit" type="button" disabled={!canSubmit} onClick={() => void handleSubmit()}>
          {submissionStatus === 'submitting' ? 'Enviando…' : 'Enviar carta'}
        </button>
      </div>
    </section>
  )
}

export default MobilePlayerTurnScreen
