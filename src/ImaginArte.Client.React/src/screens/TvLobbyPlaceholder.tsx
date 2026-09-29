import { useEffect, useState } from 'react'
import './TvLobbyPlaceholder.css'
import type { RoomStateResponse } from '../models/roomState'
import {
  getRoomState,
  RoomApiError,
  RoomsApiConfigurationError,
} from '../services/roomsApi'
import {
  createRoomRealtimeClient,
  type RoomRealtimeClient,
  type RoomRealtimeStatus,
} from '../services/roomRealtime'

interface TvLobbyPlaceholderProps {
  roomCode: string
}

interface LobbyViewState {
  roomCode: string
  retryVersion: number
  room: RoomStateResponse | null
  isLoading: boolean
  errorMessage: string | null
  connectionStatus: RoomRealtimeStatus
}

function createLoadingState(roomCode: string, retryVersion: number): LobbyViewState {
  return {
    roomCode,
    retryVersion,
    room: null,
    isLoading: true,
    errorMessage: null,
    connectionStatus: 'connecting',
  }
}

function TvLobbyPlaceholder({ roomCode }: TvLobbyPlaceholderProps) {
  const [retryVersion, setRetryVersion] = useState(0)
  const [lobbyState, setLobbyState] = useState<LobbyViewState>(() =>
    createLoadingState(roomCode, 0),
  )
  const visibleState = lobbyState.roomCode === roomCode
    && lobbyState.retryVersion === retryVersion
    ? lobbyState
    : createLoadingState(roomCode, retryVersion)

  useEffect(() => {
    const abortController = new AbortController()
    let isActive = true
    let realtimeClient: RoomRealtimeClient | null = null

    function updateCurrentState(updates: Partial<LobbyViewState>) {
      setLobbyState((currentState) => {
        if (
          currentState.roomCode !== roomCode ||
          currentState.retryVersion !== retryVersion
        ) {
          return currentState
        }

        return { ...currentState, ...updates }
      })
    }

    async function loadRoomAndSubscribe() {
      try {
        const initialRoom = await getRoomState(roomCode, abortController.signal)

        if (!isActive) {
          return
        }

        setLobbyState({
          roomCode,
          retryVersion,
          room: initialRoom,
          isLoading: false,
          errorMessage: null,
          connectionStatus: 'connecting',
        })

        realtimeClient = createRoomRealtimeClient(roomCode, {
          onRoomUpdated(updatedRoom) {
            if (isActive) {
              updateCurrentState({ room: updatedRoom })
            }
          },
          onStatusChanged(status) {
            if (!isActive) {
              return
            }

            updateCurrentState({ connectionStatus: status })
            if (status === 'connected') {
              updateCurrentState({ errorMessage: null })
            }
          },
          onError() {
            if (isActive) {
              updateCurrentState({
                errorMessage:
                'La conexión en tiempo real necesita recuperarse. Puedes reintentar.',
              })
            }
          },
        })

        await realtimeClient.start()
      } catch (error) {
        if (!isActive) {
          return
        }

        setLobbyState((currentState) => {
          const currentRoom = currentState.roomCode === roomCode
            && currentState.retryVersion === retryVersion
            ? currentState.room
            : null

          return {
            roomCode,
            retryVersion,
            room: currentRoom,
            isLoading: false,
            errorMessage: getRoomErrorMessage(error),
            connectionStatus: currentRoom ? 'disconnected' : 'connecting',
          }
        })
      } finally {
        if (isActive) {
          updateCurrentState({ isLoading: false })
        }
      }
    }

    void loadRoomAndSubscribe()

    return () => {
      isActive = false
      abortController.abort()

      if (realtimeClient) {
        void realtimeClient.stop()
      }
    }
  }, [roomCode, retryVersion])

  function handleRetry() {
    setRetryVersion((version) => version + 1)
  }

  const connectionStatusLabel = getConnectionStatusLabel(visibleState.connectionStatus)
  const canRetry = visibleState.errorMessage !== null
    || visibleState.connectionStatus === 'disconnected'

  return (
    <section
      className="tv-lobby-placeholder"
      aria-labelledby="tv-lobby-placeholder-title"
      aria-busy={visibleState.isLoading}
    >
      <div className="tv-lobby-placeholder__panel">
        <h1
          className="tv-lobby-placeholder__title"
          id="tv-lobby-placeholder-title"
        >
          Sala creada
        </h1>

        {visibleState.isLoading && (
          <p className="tv-lobby-placeholder__status" role="status">
            Cargando los datos de la sala…
          </p>
        )}

        {visibleState.errorMessage && (
          <p className="tv-lobby-placeholder__error" role="alert">
            {visibleState.errorMessage}
          </p>
        )}

        {visibleState.room && (
          <>
            <p className="tv-lobby-placeholder__label">Código de sala</p>
            <p className="tv-lobby-placeholder__code">
              {visibleState.room.roomCode}
            </p>
            <p
              className="tv-lobby-placeholder__status"
              role="status"
              aria-live="polite"
            >
              {connectionStatusLabel}
            </p>
          </>
        )}

        {canRetry && (
          <button
            className="tv-lobby-placeholder__retry"
            type="button"
            onClick={handleRetry}
          >
            Reintentar
          </button>
        )}
      </div>
    </section>
  )
}

function getRoomErrorMessage(error: unknown): string {
  if (error instanceof RoomsApiConfigurationError) {
    return 'Falta configurar la dirección del servidor en VITE_API_BASE_URL.'
  }

  if (error instanceof RoomApiError && error.status === 404) {
    return 'No se encontró la sala. Comprueba el código e inténtalo de nuevo.'
  }

  if (error instanceof RoomApiError) {
    return 'El servidor no pudo cargar la sala. Inténtalo de nuevo.'
  }

  return 'No se pudo conectar con el servidor. Comprueba la conexión e inténtalo de nuevo.'
}

function getConnectionStatusLabel(status: RoomRealtimeStatus): string {
  switch (status) {
    case 'connecting':
      return 'Conectando en tiempo real…'
    case 'connected':
      return 'Conectado en tiempo real'
    case 'reconnecting':
      return 'Reconectando…'
    case 'disconnected':
      return 'Desconectado'
  }
}

export default TvLobbyPlaceholder
