import { useRef, useState } from 'react'
import type { FormEvent } from 'react'
import CeremonialWindowDecorations from '../components/CeremonialWindowDecorations'
import './MobileJoinScreen.css'

const ROOM_CODE_PATTERN = /^[ABCDEFGHJKLMNPQRSTUVWXYZ23456789]{6}$/

interface MobileJoinScreenProps {
  roomCode: string
}

type MobileJoinField = 'roomCode' | 'playerName'

interface MobileJoinFieldErrors {
  roomCode?: string
  playerName?: string
}

function MobileJoinScreen({ roomCode }: MobileJoinScreenProps) {
  const [enteredRoomCode, setEnteredRoomCode] = useState(() =>
    roomCode.trim().toUpperCase(),
  )
  const [playerName, setPlayerName] = useState('')
  const [fieldErrors, setFieldErrors] = useState<MobileJoinFieldErrors>({})
  // La carga se activará en el Paso 3, al conectar la solicitud HTTP.
  const [isJoining] = useState(false)
  const roomCodeInputRef = useRef<HTMLInputElement>(null)
  const playerNameInputRef = useRef<HTMLInputElement>(null)

  function clearFieldError(field: MobileJoinField) {
    setFieldErrors((currentErrors) => {
      if (!currentErrors[field]) {
        return currentErrors
      }

      const nextErrors = { ...currentErrors }
      delete nextErrors[field]
      return nextErrors
    })
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (isJoining) {
      return
    }

    const normalizedRoomCode = enteredRoomCode.trim().toUpperCase()
    const normalizedPlayerName = playerName.trim()
    const nextErrors: MobileJoinFieldErrors = {}

    if (!ROOM_CODE_PATTERN.test(normalizedRoomCode)) {
      nextErrors.roomCode =
        'El código debe tener 6 caracteres válidos (A–H, J–N, P–Z y 2–9).'
    }

    if (normalizedPlayerName.length === 0) {
      nextErrors.playerName = 'Introduce tu nombre.'
    } else if (normalizedPlayerName.length > 40) {
      nextErrors.playerName = 'El nombre debe tener entre 1 y 40 caracteres.'
    }

    setEnteredRoomCode(normalizedRoomCode)
    setPlayerName(normalizedPlayerName)
    setFieldErrors(nextErrors)

    if (nextErrors.roomCode) {
      roomCodeInputRef.current?.focus()
      return
    }

    if (nextErrors.playerName) {
      playerNameInputRef.current?.focus()
    }
    // A valid form stays here; sending the join request is Step 3.
  }

  return (
    <main className="mobile-join" aria-labelledby="mobile-join-title">
      <div className="mobile-join__backdrop" aria-hidden="true" />

      <div className="mobile-join__frame" aria-hidden="true">
        <CeremonialWindowDecorations />
      </div>

      <div
        className="mobile-join__scroll-region"
        role="region"
        aria-label="Contenido para unirse a la sala"
        tabIndex={0}
      >
        <section className="mobile-join__panel">
          <header className="mobile-join__header">
            <p className="mobile-join__eyebrow">ImaginArte · Control móvil</p>
            <h1 className="mobile-join__title" id="mobile-join-title">
              Unirse a sala
            </h1>
            <p className="mobile-join__introduction">
              Escribe el código que aparece en la televisión y el nombre con el
              que te reconocerán.
            </p>
          </header>

          <form
            className="mobile-join__form"
            noValidate
            onSubmit={handleSubmit}
            aria-busy={isJoining}
          >
            <div className="mobile-join__field">
              <label htmlFor="mobile-join-room-code">Código de sala</label>
              <input
                autoCapitalize="characters"
                autoComplete="off"
                aria-describedby={fieldErrors.roomCode
                  ? 'mobile-join-room-code-error'
                  : undefined}
                aria-invalid={Boolean(fieldErrors.roomCode)}
                className="mobile-join__input mobile-join__input--code"
                disabled={isJoining}
                id="mobile-join-room-code"
                name="roomCode"
                onChange={(event) => {
                  setEnteredRoomCode(event.currentTarget.value)
                  clearFieldError('roomCode')
                }}
                placeholder="ABC234"
                ref={roomCodeInputRef}
                required
                type="text"
                value={enteredRoomCode}
              />
              {fieldErrors.roomCode && (
                <p
                  className="mobile-join__field-error"
                  id="mobile-join-room-code-error"
                  role="alert"
                >
                  {fieldErrors.roomCode}
                </p>
              )}
            </div>

            <div className="mobile-join__field">
              <label htmlFor="mobile-join-player-name">Tu nombre</label>
              <input
                autoComplete="name"
                aria-describedby={fieldErrors.playerName
                  ? 'mobile-join-player-name-error'
                  : undefined}
                aria-invalid={Boolean(fieldErrors.playerName)}
                className="mobile-join__input"
                disabled={isJoining}
                id="mobile-join-player-name"
                name="playerName"
                onChange={(event) => {
                  setPlayerName(event.currentTarget.value)
                  clearFieldError('playerName')
                }}
                placeholder="Cómo te llamas"
                ref={playerNameInputRef}
                required
                type="text"
                value={playerName}
              />
              {fieldErrors.playerName && (
                <p
                  className="mobile-join__field-error"
                  id="mobile-join-player-name-error"
                  role="alert"
                >
                  {fieldErrors.playerName}
                </p>
              )}
            </div>

            <button
              aria-busy={isJoining}
              className="mobile-join__submit"
              disabled={isJoining}
              type="submit"
            >
              {isJoining ? 'Uniéndote…' : 'Unirme a la sala'}
            </button>
          </form>

          <p className="mobile-join__privacy">
            <span aria-hidden="true">✦</span>
            Esta pantalla es privada; tus cartas no se mostrarán aquí en la TV.
          </p>
        </section>
      </div>
    </main>
  )
}

export default MobileJoinScreen
