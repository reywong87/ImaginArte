import { useEffect, useRef, useState } from 'react'
import { QRCodeSVG } from 'qrcode.react'
import {
  RoomPhase,
  type RoomStateResponse,
} from '../models/roomState'
import {
  getRoomState,
  RoomApiError,
  RoomsApiConfigurationError,
  startRoom,
  updateRoomTargetScore,
} from '../services/roomsApi'
import {
  createRoomRealtimeClient,
  type RoomRealtimeClient,
  type RoomRealtimeStatus,
} from '../services/roomRealtime'
import { assignPlayerAvatars } from '../services/playerAvatarAssignments'
import CeremonialWindowDecorations from '../components/CeremonialWindowDecorations'
import TvWaitingForStorytellerScreen from './TvWaitingForStorytellerScreen'
import './TvLobbyScreen.css'

interface TvLobbyScreenProps {
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

interface AvatarViewState {
  roomCode: string
  retryVersion: number
  assignments: Record<string, number>
}

interface TargetScoreSaveState {
  roomCode: string
  retryVersion: number
  isSaving: boolean
  errorMessage: string | null
  failedTargetScore: number | null
}

interface StartGameState {
  roomCode: string
  retryVersion: number
  isStarting: boolean
  errorMessage: string | null
}

const TARGET_SCORE_OPTIONS = [
  { score: 15, label: 'Corta', detail: '15 puntos' },
  { score: 25, label: 'Media', detail: '25 puntos' },
  { score: 35, label: 'Larga', detail: '35 puntos' },
] as const

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

function TvLobbyScreen({ roomCode }: TvLobbyScreenProps) {
  const [retryVersion, setRetryVersion] = useState(0)
  const [lobbyState, setLobbyState] = useState<LobbyViewState>(() =>
    createLoadingState(roomCode, 0),
  )
  const [avatarState, setAvatarState] = useState<AvatarViewState | null>(null)
  const [targetScoreSaveState, setTargetScoreSaveState] =
    useState<TargetScoreSaveState | null>(null)
  const [startGameState, setStartGameState] = useState<StartGameState | null>(null)
  const startRequestsInFlight = useRef(new Set<string>())

  const visibleState = lobbyState.roomCode === roomCode
    && lobbyState.retryVersion === retryVersion
    ? lobbyState
    : createLoadingState(roomCode, retryVersion)
  const visibleAvatarAssignments = avatarState?.roomCode === roomCode
    && avatarState.retryVersion === retryVersion
    ? avatarState.assignments
    : {}
  const visibleTargetScoreSaveState = targetScoreSaveState?.roomCode === roomCode
    && targetScoreSaveState.retryVersion === retryVersion
    ? targetScoreSaveState
    : {
        roomCode,
        retryVersion,
        isSaving: false,
        errorMessage: null,
        failedTargetScore: null,
      }
  const visibleStartGameState = startGameState?.roomCode === roomCode
    && startGameState.retryVersion === retryVersion
    ? startGameState
    : {
        roomCode,
        retryVersion,
        isStarting: false,
        errorMessage: null,
      }

  useEffect(() => {
    const abortController = new AbortController()
    let isActive = true
    let realtimeClient: RoomRealtimeClient | null = null

    function updateCurrentState(updates: Partial<LobbyViewState>) {
      setLobbyState((currentState) => {
        if (
          currentState.roomCode !== roomCode
          || currentState.retryVersion !== retryVersion
        ) {
          return currentState
        }

        return { ...currentState, ...updates }
      })
    }

    function applyRoomSnapshot(updatedRoom: RoomStateResponse) {
      if (!isActive) {
        return
      }

      updateCurrentState({
        room: updatedRoom,
        isLoading: false,
        errorMessage: null,
      })
      setAvatarState({
        roomCode,
        retryVersion,
        assignments: assignPlayerAvatars(roomCode, updatedRoom.players),
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
        setAvatarState({
          roomCode,
          retryVersion,
          assignments: assignPlayerAvatars(roomCode, initialRoom.players),
        })

        realtimeClient = createRoomRealtimeClient(roomCode, {
          onRoomUpdated: applyRoomSnapshot,
          onStatusChanged(status) {
            if (!isActive) {
              return
            }

            updateCurrentState({
              connectionStatus: status,
              ...(status === 'connected' ? { errorMessage: null } : {}),
            })
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
            errorMessage: getLobbyErrorMessage(error),
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

  async function saveTargetScore(targetScore: number) {
    const room = visibleState.room

    if (
      !room
      || room.phase !== RoomPhase.WaitingForPlayers
      || visibleTargetScoreSaveState.isSaving
    ) {
      return
    }

    const requestState: TargetScoreSaveState = {
      roomCode,
      retryVersion,
      isSaving: true,
      errorMessage: null,
      failedTargetScore: null,
    }
    setTargetScoreSaveState(requestState)

    try {
      const updatedRoom = await updateRoomTargetScore(roomCode, targetScore)

      setLobbyState((currentState) => {
        if (
          currentState.roomCode !== roomCode
          || currentState.retryVersion !== retryVersion
        ) {
          return currentState
        }

        return { ...currentState, room: updatedRoom }
      })
      setAvatarState({
        roomCode,
        retryVersion,
        assignments: assignPlayerAvatars(roomCode, updatedRoom.players),
      })
      setTargetScoreSaveState({
        ...requestState,
        isSaving: false,
      })
    } catch (error) {
      setTargetScoreSaveState({
        ...requestState,
        isSaving: false,
        errorMessage: getTargetScoreErrorMessage(error),
        failedTargetScore: targetScore,
      })
    }
  }

  const room = visibleState.room
  const mobileJoinBaseUrl = import.meta.env.VITE_MOBILE_JOIN_BASE_URL?.trim() ?? ''
  const mobileJoinUrl = room
    ? buildMobileJoinUrl(mobileJoinBaseUrl, room.roomCode)
    : null
  const mobileJoinConfigurationError = mobileJoinBaseUrl.length === 0
    ? 'Falta configurar VITE_MOBILE_JOIN_BASE_URL. Puedes unirte con el código de sala.'
    : mobileJoinUrl === null
      ? 'La dirección móvil configurada no es válida. Puedes unirte con el código de sala.'
      : null
  const isWaitingForPlayers = room?.phase === RoomPhase.WaitingForPlayers
  const playerCount = room?.players.length ?? 0
  const isRoomFull = room !== null && playerCount >= room.maximumPlayers
  const connectionStatusLabel = getConnectionStatusLabel(visibleState.connectionStatus)
  const canStartGame = room !== null
    && isWaitingForPlayers
    && playerCount >= room.minimumPlayers
  const startButtonDisabled = !canStartGame || visibleStartGameState.isStarting
  const startButtonDescription = visibleStartGameState.isStarting
    ? 'La solicitud de inicio está en curso.'
    : !isWaitingForPlayers
      ? 'La partida ya comenzó.'
      : room !== null && playerCount < room.minimumPlayers
        ? `Se necesitan al menos ${room.minimumPlayers} jugadores para iniciar. Hay ${playerCount}.`
        : visibleStartGameState.errorMessage
          ? 'El inicio no se completó. Puedes volver a intentarlo.'
          : 'Inicia la partida desde la televisión.'

  async function handleStartGame() {
    const currentRoom = room

    if (
      !currentRoom
      || !canStartGame
      || visibleStartGameState.isStarting
      || startRequestsInFlight.current.has(roomCode)
    ) {
      return
    }

    startRequestsInFlight.current.add(roomCode)
    const pendingState: StartGameState = {
      roomCode,
      retryVersion,
      isStarting: true,
      errorMessage: null,
    }
    setStartGameState(pendingState)

    try {
      const startedRoom = await startRoom(roomCode)
      setLobbyState((currentState) => {
        if (
          currentState.roomCode !== roomCode
          || currentState.retryVersion !== retryVersion
        ) {
          return currentState
        }

        return {
          ...currentState,
          room: startedRoom,
          isLoading: false,
          errorMessage: null,
        }
      })
      setStartGameState({ ...pendingState, isStarting: false })
    } catch (error) {
      let recoveredRoom: RoomStateResponse | null = null

      try {
        recoveredRoom = await getRoomState(roomCode)
      } catch {
        // Keep the start error visible if the authoritative snapshot is also unavailable.
      }

      if (recoveredRoom) {
        const snapshot = recoveredRoom
        setLobbyState((currentState) => {
          if (
            currentState.roomCode !== roomCode
            || currentState.retryVersion !== retryVersion
          ) {
            return currentState
          }

          return {
            ...currentState,
            room: snapshot,
            isLoading: false,
          }
        })
      }

      const startWasApplied = recoveredRoom !== null
        && recoveredRoom.phase !== RoomPhase.WaitingForPlayers
      setStartGameState({
        ...pendingState,
        isStarting: false,
        errorMessage: startWasApplied ? null : getStartGameErrorMessage(error),
      })
    } finally {
      startRequestsInFlight.current.delete(roomCode)
    }
  }

  return (
    <main
      className="tv-lobby-screen"
      aria-busy={visibleState.isLoading}
      aria-labelledby="tv-lobby-title"
    >
      <div className="tv-lobby-screen__content">
        <header className="tv-lobby-screen__header">
          <p className="tv-lobby-screen__eyebrow">ImaginArte · Sala familiar</p>
          <h1 className="tv-lobby-screen__title" id="tv-lobby-title">
            {room?.phase === RoomPhase.StorytellerChoosing
              ? 'El narrador prepara la historia'
              : room
                ? 'Todo empieza con una historia'
                : 'Abriendo la sala'}
          </h1>
          {room && (
            <p
              className={`tv-lobby-screen__connection tv-lobby-screen__connection--${visibleState.connectionStatus}`}
              role="status"
              aria-live="polite"
            >
              <span className="tv-lobby-screen__connection-dot" aria-hidden="true" />
              {connectionStatusLabel}
            </p>
          )}
        </header>

        {visibleState.isLoading && (
          <p className="tv-lobby-screen__notice" role="status">
            Cargando los datos de la sala…
          </p>
        )}

        {visibleState.errorMessage && (
          <div className="tv-lobby-screen__error" role="alert">
            <p>{visibleState.errorMessage}</p>
            <button
              className="tv-lobby-screen__button tv-lobby-screen__button--secondary"
              type="button"
              onClick={handleRetry}
            >
              Reintentar conexión
            </button>
          </div>
        )}

        {room?.phase === RoomPhase.StorytellerChoosing ? (
          <TvWaitingForStorytellerScreen room={room} />
        ) : room && (
          <>
            <div className="tv-lobby-screen__main-grid">
              <section
                className="tv-lobby-panel tv-lobby-panel--join"
                aria-labelledby="tv-lobby-join-title"
              >
                <CeremonialWindowDecorations />
                <h2 className="tv-lobby-panel__title" id="tv-lobby-join-title">
                  Únete a la sala
                </h2>

                <div className="tv-lobby-screen__room-code-display">
                  <span
                    className="tv-lobby-screen__room-code-ornament"
                    aria-hidden="true"
                  />
                  <p className="tv-lobby-screen__room-code">{room.roomCode}</p>
                  <span
                    className="tv-lobby-screen__room-code-ornament tv-lobby-screen__room-code-ornament--right"
                    aria-hidden="true"
                  />
                </div>
                <p className="tv-lobby-screen__join-instruction">
                  Escanea el QR o introduce este código en el móvil.
                </p>

                {mobileJoinUrl ? (
                  <div className="tv-lobby-screen__qr-stage">
                    <span
                      className="tv-lobby-screen__qr-ornament"
                      aria-hidden="true"
                    />
                    <div className="tv-lobby-screen__qr-frame">
                      <QRCodeSVG
                        value={mobileJoinUrl}
                        size={256}
                        level="M"
                        marginSize={4}
                        bgColor="#FBF4E2"
                        fgColor="#10243A"
                        title={`Código QR para unirse a la sala ${room.roomCode}`}
                      />
                    </div>
                    <span
                      className="tv-lobby-screen__qr-ornament tv-lobby-screen__qr-ornament--right"
                      aria-hidden="true"
                    />
                  </div>
                ) : (
                  <p className="tv-lobby-screen__configuration-warning" role="status">
                    {mobileJoinConfigurationError}
                  </p>
                )}
              </section>

              <section
                className="tv-lobby-panel tv-lobby-panel--players"
                aria-labelledby="tv-lobby-players-title"
              >
                <CeremonialWindowDecorations />
                <div className="tv-lobby-panel__heading">
                  <h2 className="tv-lobby-panel__title" id="tv-lobby-players-title">
                    Jugadores conectados
                  </h2>
                  <p className="tv-lobby-screen__player-count">
                    <span className="visually-hidden">Jugadores en la sala: </span>
                    {playerCount} / {room.maximumPlayers}
                  </p>
                </div>

                {isRoomFull && (
                  <p className="tv-lobby-screen__full-message" role="status">
                    Sala completa
                  </p>
                )}

                <ol
                  className="tv-lobby-screen__player-list"
                  aria-label="Plazas de la sala"
                >
                  {room.players.map((player) => {
                    const avatarIndex = visibleAvatarAssignments[player.playerId]
                    const isHost = player.playerId.toLowerCase()
                      === room.hostPlayerId?.toLowerCase()
                    const statusLabel = player.isConnected
                      ? 'Conectado'
                      : 'Reconectando'

                    return (
                      <li className="tv-lobby-screen__player" key={player.playerId}>
                        <span
                          className="tv-lobby-screen__avatar"
                          style={getAvatarStyle(avatarIndex)}
                          aria-hidden="true"
                        />
                        <span className="tv-lobby-screen__player-name">
                          {player.name}
                        </span>
                        {isHost && (
                          <span className="tv-lobby-screen__host-badge">
                            <span aria-hidden="true">✦</span>
                            Anfitrión
                          </span>
                        )}
                        <span
                          className={`tv-lobby-screen__player-status tv-lobby-screen__player-status--${player.isConnected ? 'connected' : 'reconnecting'}`}
                        >
                          <span
                            className="tv-lobby-screen__player-status-dot"
                            aria-hidden="true"
                          />
                          {statusLabel}
                        </span>
                      </li>
                    )
                  })}
                  {Array.from(
                    { length: Math.max(room.maximumPlayers - playerCount, 0) },
                    (_, slotIndex) => (
                      <li
                        className="tv-lobby-screen__player tv-lobby-screen__player--placeholder"
                        key={`empty-slot-${slotIndex}`}
                      >
                        <span
                          className="tv-lobby-screen__avatar tv-lobby-screen__avatar--placeholder"
                          aria-hidden="true"
                        />
                        <span className="tv-lobby-screen__placeholder-label">
                          Esperando jugadores…
                        </span>
                      </li>
                    ),
                  )}
                </ol>
              </section>

            <section
              className="tv-lobby-panel tv-lobby-panel--target-score"
              aria-labelledby="target-score-title"
            >
              <CeremonialWindowDecorations />
              <div className="tv-lobby-panel__heading">
                <div>
                  <p className="tv-lobby-screen__eyebrow">Elige el viaje</p>
                  <h2 className="tv-lobby-panel__title" id="target-score-title">
                    Duración de la partida
                  </h2>
                </div>
              </div>

              <fieldset
                className="tv-lobby-screen__score-options"
                disabled={!isWaitingForPlayers || visibleTargetScoreSaveState.isSaving}
              >
                <legend className="visually-hidden">Elige la duración de la partida</legend>
                {TARGET_SCORE_OPTIONS.map((option) => (
                  <label
                    className="tv-lobby-screen__score-option"
                    key={option.score}
                  >
                    <input
                      type="radio"
                      name={`target-score-${room.roomCode}`}
                      value={option.score}
                      checked={room.targetScore === option.score}
                      onChange={() => void saveTargetScore(option.score)}
                    />
                    <span className="tv-lobby-screen__score-option-copy">
                      <span className="tv-lobby-screen__score-option-label">
                        {option.label}
                      </span>
                      <span className="tv-lobby-screen__score-option-detail">
                        {option.detail}
                      </span>
                    </span>
                  </label>
                ))}
              </fieldset>

              {!isWaitingForPlayers && (
                <p className="tv-lobby-screen__score-note">
                  El modo ya no se puede cambiar porque la partida comenzó.
                </p>
              )}

              {visibleTargetScoreSaveState.isSaving && (
                <p className="tv-lobby-screen__score-saving" role="status">
                  Guardando el modo elegido…
                </p>
              )}

              {visibleTargetScoreSaveState.errorMessage && (
                <div className="tv-lobby-screen__score-error" role="alert">
                  <p>{visibleTargetScoreSaveState.errorMessage}</p>
                  {isWaitingForPlayers
                    && visibleTargetScoreSaveState.failedTargetScore !== null && (
                    <button
                      className="tv-lobby-screen__button tv-lobby-screen__button--secondary"
                      type="button"
                      onClick={() => {
                        const failedTargetScore =
                          visibleTargetScoreSaveState.failedTargetScore

                        if (failedTargetScore !== null) {
                          void saveTargetScore(failedTargetScore)
                        }
                      }}
                    >
                      Reintentar guardado
                    </button>
                  )}
                </div>
              )}
            </section>

            <div className="tv-lobby-screen__start-region">
              {visibleStartGameState.errorMessage && (
                <div className="tv-lobby-screen__start-error" role="alert">
                  <p>{visibleStartGameState.errorMessage}</p>
                </div>
              )}
              <span
                className="visually-hidden"
                id="tv-lobby-screen-start-description"
              >
                {startButtonDescription}
              </span>
              {visibleStartGameState.isStarting && (
                <span className="visually-hidden" role="status" aria-live="polite">
                  Iniciando la partida…
                </span>
              )}
              <button
                className="tv-lobby-screen__start-action"
                type="button"
                disabled={startButtonDisabled}
                aria-describedby="tv-lobby-screen-start-description"
                aria-busy={visibleStartGameState.isStarting}
                onClick={() => void handleStartGame()}
              >
                <span
                  className="tv-lobby-screen__start-ornament"
                  aria-hidden="true"
                />
                <span className="tv-lobby-screen__start-label">
                  {visibleStartGameState.isStarting
                    ? 'Iniciando…'
                    : visibleStartGameState.errorMessage
                      ? 'Reintentar inicio'
                      : 'Iniciar experiencia'}
                </span>
                <span
                  className="tv-lobby-screen__start-ornament tv-lobby-screen__start-ornament--right"
                  aria-hidden="true"
                />
              </button>
            </div>
            </div>
          </>
        )}
      </div>
    </main>
  )
}

function buildMobileJoinUrl(baseUrl: string, roomCode: string): string | null {
  if (!baseUrl) {
    return null
  }

  try {
    const configuredUrl = new URL(baseUrl)

    if (configuredUrl.protocol !== 'http:' && configuredUrl.protocol !== 'https:') {
      return null
    }

    configuredUrl.pathname = `/join/${encodeURIComponent(roomCode)}`
    configuredUrl.search = ''
    configuredUrl.hash = ''
    return configuredUrl.toString()
  } catch {
    return null
  }
}

function getAvatarStyle(avatarIndex: number | undefined) {
  if (avatarIndex === undefined) {
    return undefined
  }

  const column = avatarIndex % 4
  const row = Math.floor(avatarIndex / 4)

  return {
    backgroundPosition: `${(column / 3) * 100}% ${(row / 3) * 100}%`,
  }
}

function getLobbyErrorMessage(error: unknown): string {
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

function getTargetScoreErrorMessage(error: unknown): string {
  if (error instanceof RoomsApiConfigurationError) {
    return 'Falta configurar la dirección del servidor. No se cambió el modo confirmado.'
  }

  if (error instanceof RoomApiError && error.status === 404) {
    return 'No se encontró la sala. No se cambió el modo confirmado.'
  }

  if (error instanceof RoomApiError && error.status === 400) {
    return 'La partida ya comenzó. Se conserva el último modo confirmado.'
  }

  return 'No se pudo guardar el modo. Se conserva el último modo confirmado.'
}

function getStartGameErrorMessage(error: unknown): string {
  if (error instanceof RoomsApiConfigurationError) {
    return 'Falta configurar la dirección del servidor. No se inició la partida.'
  }

  if (error instanceof RoomApiError && error.status === 404) {
    return 'No se encontró la sala. No se inició la partida.'
  }

  if (error instanceof RoomApiError && error.status === 400) {
    return 'No se pudo iniciar la partida. Comprueba que la sala sigue esperando y que hay suficientes jugadores.'
  }

  return 'No se pudo confirmar el inicio. Se actualizó la sala; puedes volver a intentarlo si sigue esperando.'
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

export default TvLobbyScreen
