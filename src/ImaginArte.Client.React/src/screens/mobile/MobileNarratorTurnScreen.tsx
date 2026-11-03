import { useEffect, useRef, useState } from 'react'
import { RoomPhase, type GetPlayerPrivateStateResponse, type PrivateCardStateResponse, type RoomStateResponse } from '../../models/roomState'
import { resolveCardAsset, type CardAssetResolution } from '../../services/cardAssets'
import { chooseStorytellerCard, RoomApiError } from '../../services/roomsApi'
import './MobileNarratorTurnScreen.css'

interface MobileNarratorTurnScreenProps {
  hand: PrivateCardStateResponse[]
  roomCode: string
  playerId: string
  connected: boolean
  onRetryConnection: (() => void) | null
  onConfirmed: (room: RoomStateResponse) => void
  onRecover: () => Promise<GetPlayerPrivateStateResponse>
}

function MobileNarratorTurnScreen({ hand, roomCode, playerId, connected, onRetryConnection, onConfirmed, onRecover }: MobileNarratorTurnScreenProps) {
  const [assets, setAssets] = useState<Record<string, CardAssetResolution>>({})
  const [selectedCardId, setSelectedCardId] = useState<string | null>(null)
  const [clue, setClue] = useState('')
  const [submissionStatus, setSubmissionStatus] = useState<'idle' | 'submitting' | 'checking' | 'uncertain'>('idle')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const inFlight = useRef(false)
  const mounted = useRef(true)
  const handIds = JSON.stringify(hand.map((card) => card.cardId))
  const canSubmit = connected && submissionStatus === 'idle'
    && selectedCardId !== null
    && hand.some((card) => card.cardId === selectedCardId)
    && assets[selectedCardId]?.isSelectable === true
    && clue.trim().length > 0

  useEffect(() => {
    mounted.current = true
    return () => { mounted.current = false }
  }, [])

  async function checkSubmission() {
    setSubmissionStatus('checking')
    try {
      const state = await onRecover()
      if (!mounted.current) return
      if (state.player.isCurrentStoryteller && state.room.phase === RoomPhase.StorytellerChoosing) {
        setErrorMessage('No se confirmó el envío. Puedes revisar tu borrador y volver a intentarlo.')
        setSubmissionStatus('idle')
      }
    } catch {
      if (mounted.current) {
        setErrorMessage('No se pudo comprobar el envío. Comprueba el estado antes de volver a enviar.')
        setSubmissionStatus('uncertain')
      }
    }
  }

  async function handleSubmit() {
    if (!canSubmit || selectedCardId === null || inFlight.current) return
    inFlight.current = true
    setSubmissionStatus('submitting')
    setErrorMessage(null)
    try {
      const room = await chooseStorytellerCard(roomCode, playerId, selectedCardId, clue.trim())
      if (!mounted.current) return
      if (room.phase === RoomPhase.StorytellerChoosing) {
        await checkSubmission()
      } else {
        onConfirmed(room)
      }
    } catch (error) {
      if (!mounted.current) return
      if (error instanceof RoomApiError && error.status >= 400 && error.status < 500) {
        setErrorMessage(`No se pudo enviar la carta (HTTP ${error.status}). Conservamos tu borrador.`)
        setSubmissionStatus('idle')
      } else {
        await checkSubmission()
      }
    } finally {
      inFlight.current = false
    }
  }

  async function handleCheckAgain() {
    if (inFlight.current) return
    inFlight.current = true
    try {
      await checkSubmission()
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

    return () => {
      isCurrent = false
    }
  }, [handIds])

  return (
    <section className="narrator-turn" aria-labelledby="mobile-join-title">
      <header className="narrator-turn__header">
        <p className="narrator-turn__eyebrow">Tu turno</p>
        <h1 id="mobile-join-title">Elige una carta y crea una pista</h1>
        <p>La pista debe ayudar a algunos jugadores, pero no a todos</p>
      </header>

      <fieldset className="narrator-turn__hand">
        <legend>Tu mano privada</legend>
        <div className="narrator-turn__cards" tabIndex={0} role="region" aria-label="Cartas de tu mano">
          {hand.map((card, index) => {
            const asset = assets[card.cardId]
            const selected = selectedCardId === card.cardId && asset?.isSelectable === true
            const available = asset?.isSelectable === true && asset.imageUrl !== null

            return (
              <label className={`narrator-turn__card${selected ? ' narrator-turn__card--selected' : ''}`} key={card.cardId}>
                <input
                  type="radio"
                  name="narrator-card"
                  value={card.cardId}
                  checked={selected}
                  disabled={!available || submissionStatus !== 'idle'}
                  onChange={() => setSelectedCardId(card.cardId)}
                  aria-label={`Carta ${index + 1}: ${asset?.alt ?? 'Cargando ilustración'}`}
                />
                <span className="narrator-turn__art">
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
                  ) : (
                    <span>{asset?.availabilityMessage ?? 'Cargando ilustración…'}</span>
                  )}
                </span>
                <span className="narrator-turn__card-label">
                  {selected ? '✓ Seleccionada' : available ? 'Elegir carta' : asset ? 'No disponible' : 'Cargando'}
                </span>
              </label>
            )
          })}
        </div>
      </fieldset>

      <div className="narrator-turn__controls">
        <label htmlFor="narrator-clue">Tu pista</label>
        <input
          id="narrator-clue"
          className="mobile-join__input"
          type="text"
          value={clue}
          disabled={submissionStatus !== 'idle'}
          onChange={(event) => setClue(event.currentTarget.value)}
        />
        <p>No se mostrará qué carta elegiste.</p>
        {submissionStatus === 'submitting' && <p role="status">Enviando carta y pista…</p>}
        {submissionStatus === 'checking' && <p role="status">Comprobando el estado de la sala…</p>}
        {!connected && <p role="status">Esperando la conexión con la sala…</p>}
        {onRetryConnection && <button className="mobile-join__snapshot-retry" type="button" onClick={onRetryConnection}>Reintentar conexión</button>}
        {errorMessage && <p className="mobile-join__form-error" role="alert">{errorMessage}</p>}
        {submissionStatus === 'uncertain' && (
          <button className="mobile-join__snapshot-retry" type="button" onClick={() => void handleCheckAgain()}>
            Comprobar estado
          </button>
        )}
        <button className="mobile-join__submit" type="button" disabled={!canSubmit} onClick={() => void handleSubmit()}>
          {submissionStatus === 'submitting' ? 'Enviando…' : 'Enviar carta y pista'}
        </button>
      </div>
    </section>
  )
}

export default MobileNarratorTurnScreen
