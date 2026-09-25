import { useState } from 'react'
import './TvHomeScreen.css'
import seedIllustration from '../assets/semilla-circulo-estrellas.png'
import { createRoomCode } from '../services/roomCode'
import {
  createRoom,
  RoomApiError,
  RoomsApiConfigurationError,
} from '../services/roomsApi'

interface TvHomeScreenProps {
  onRoomCreated: (roomCode: string) => void
}

const CURRENT_ROOM_CODE_KEY = 'imaginarte.tv.currentRoomCode'

function getCreationErrorMessage(error: unknown): string {
  if (error instanceof RoomsApiConfigurationError) {
    return 'Falta configurar la dirección del servidor en VITE_API_BASE_URL.'
  }

  if (error instanceof RoomApiError && error.status === 409) {
    return 'Ese código ya está en uso. Pulsa «Crear una sala» para intentarlo con otro.'
  }

  if (error instanceof RoomApiError) {
    return 'El servidor no pudo crear la sala. Inténtalo de nuevo.'
  }

  return 'No se pudo conectar con el servidor. Comprueba la conexión e inténtalo de nuevo.'
}

function TvHomeScreen({ onRoomCreated }: TvHomeScreenProps) {
  const [isCreating, setIsCreating] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  async function handleCreateRoom() {
    if (isCreating) {
      return
    }

    setIsCreating(true)
    setErrorMessage(null)

    try {
      const room = await createRoom(createRoomCode())
      window.sessionStorage.setItem(CURRENT_ROOM_CODE_KEY, room.roomCode)
      onRoomCreated(room.roomCode)
    } catch (error) {
      setErrorMessage(getCreationErrorMessage(error))
      setIsCreating(false)
    }
  }

  return (
    <section className="tv-home" aria-labelledby="tv-home-title">
      <div className="tv-home__content">
        <h1 className="tv-home__title" id="tv-home-title">
          ImaginArte
        </h1>

        <button
          className="room-action"
          type="button"
          aria-labelledby="create-room-title"
          aria-describedby="create-room-description"
          aria-busy={isCreating}
          disabled={isCreating}
          onClick={handleCreateRoom}
        >
          <span className="room-action__texture" aria-hidden="true" />
          <span
            className="room-action__corner room-action__corner--top-left"
            aria-hidden="true"
          />
          <span
            className="room-action__corner room-action__corner--top-right"
            aria-hidden="true"
          />
          <span
            className="room-action__corner room-action__corner--bottom-left"
            aria-hidden="true"
          />
          <span
            className="room-action__corner room-action__corner--bottom-right"
            aria-hidden="true"
          />

          <img
            className="room-action__illustration"
            src={seedIllustration}
            alt=""
            aria-hidden="true"
          />

          <span className="room-action__copy">
            <span className="room-action__title" id="create-room-title">
              Crear una sala
            </span>
            <span
              className="room-action__description"
              id="create-room-description"
            >
              Prepara una nueva experiencia
            </span>
          </span>
        </button>

        {isCreating && (
          <p className="room-action__status" role="status" aria-live="polite">
            Creando la sala…
          </p>
        )}

        {errorMessage && (
          <p className="room-action__error" role="alert">
            {errorMessage}
          </p>
        )}
      </div>
    </section>
  )
}

export default TvHomeScreen
