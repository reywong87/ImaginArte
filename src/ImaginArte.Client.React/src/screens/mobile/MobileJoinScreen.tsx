import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import CeremonialWindowDecorations from '../../components/CeremonialWindowDecorations'
import {
  clearMobilePlayerSession,
  createMobilePlayerId,
  MobilePlayerSessionStorageError,
  readMobilePlayerSession,
  writeMobilePlayerSession,
  type StoredMobilePlayerSession,
} from '../../services/mobilePlayerSession'
import {
  getRoomState,
  getPlayerPrivateState,
  joinRoomAsPlayer,
  RoomApiError,
  RoomsApiConfigurationError,
} from '../../services/roomsApi'
import type { RoomStateResponse } from '../../models/roomState'
import {
  createMobileRoomRealtimeClient,
  type MobileRoomRealtimeStatus,
} from '../../services/mobileRoomRealtime'
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

type MobileJoinStatus = 'idle' | 'recovering' | 'joining' | 'joined'
type JoinOperation = 'recovery' | 'join'

type RoomSnapshotState =
  | { roomCode: string; status: 'loaded'; snapshot: RoomStateResponse }
  | { roomCode: string; status: 'error'; message: string }

interface JoinedPlayerSummary {
  roomCode: string
  playerId: string
  playerName: string
}

function getRealtimeStatusMessage(
  status: MobileRoomRealtimeStatus | null,
  hasError: boolean,
): string | null {
  if (hasError || status === 'disconnected') {
    return 'Se interrumpió la conexión en tiempo real. Tu unión sigue guardada.'
  }

  if (status === 'connecting') {
    return 'Conectando con la sala…'
  }

  if (status === 'reconnecting') {
    return 'Conexión interrumpida. Reconectando…'
  }

  return null
}

function isNotFoundError(error: unknown): error is RoomApiError {
  return error instanceof RoomApiError && error.status === 404
}

function isAbortError(error: unknown): boolean {
  return error instanceof Error && error.name === 'AbortError'
}

function getRoomSnapshotErrorMessage(error: unknown): string {
  if (error instanceof RoomsApiConfigurationError) {
    return 'Falta configurar la dirección del servidor en VITE_API_BASE_URL.'
  }

  if (error instanceof RoomApiError && error.status === 404) {
    return 'No encontramos esa sala. Comprueba el código e inténtalo de nuevo.'
  }

  if (error instanceof TypeError) {
    return 'No se pudo conectar con el servidor. Comprueba la conexión e inténtalo de nuevo.'
  }

  return 'No se pudo consultar la sala. Inténtalo de nuevo.'
}

function getJoinErrorMessage(
  error: unknown,
  operation: JoinOperation,
): string {
  if (error instanceof MobilePlayerSessionStorageError) {
    return error.message
  }

  if (error instanceof RoomsApiConfigurationError) {
    return 'Falta configurar la dirección del servidor en VITE_API_BASE_URL.'
  }

  if (error instanceof RoomApiError) {
    if (operation === 'join' && error.status === 404) {
      return 'No encontramos esa sala. Comprueba el código e inténtalo de nuevo.'
    }

    if (error.status === 400) {
      return 'La sala ya no acepta jugadores. Puede estar llena o la partida puede haber comenzado.'
    }

    if (error.status === 409) {
      return 'El servidor rechazó la unión. Comprueba que la sala siga esperando jugadores.'
    }
  }

  if (operation === 'recovery') {
    return 'No se pudo comprobar la sesión guardada. Conservamos tus datos; pulsa «Reintentar».'
  }

  if (error instanceof TypeError) {
    return 'No se pudo conectar con el servidor. Conservamos tus datos para reintentar sin crear otra identidad.'
  }

  return 'No se pudo confirmar la unión. Conservamos tus datos para reintentar sin crear otra identidad.'
}

async function requestPlayerJoin(
  session: StoredMobilePlayerSession,
  signal?: AbortSignal,
): Promise<JoinedPlayerSummary> {
  const room = await joinRoomAsPlayer(
    session.roomCode,
    session.playerId,
    session.playerName,
    signal,
  )
  const player = room.players.find(
    (candidate) => candidate.playerId.toLowerCase() === session.playerId.toLowerCase(),
  )

  if (!player) {
    throw new Error('El servidor no confirmó la identidad del jugador unido.')
  }

  return {
    roomCode: room.roomCode,
    playerId: player.playerId,
    playerName: player.name,
  }
}

function persistJoinedSession(
  session: StoredMobilePlayerSession,
  player: JoinedPlayerSummary,
): { player: JoinedPlayerSummary; warning: string | null } {
  const joinedSession: StoredMobilePlayerSession = {
    ...session,
    roomCode: player.roomCode,
    playerId: player.playerId,
    playerName: player.playerName,
    status: 'joined',
  }
  let warning: string | null = null

  try {
    writeMobilePlayerSession(joinedSession)
  } catch {
    warning = 'Te has unido, pero el navegador no pudo actualizar la sesión guardada. Mantén esta pestaña abierta; al volver a entrar intentaremos verificar la unión.'
  }

  return { player, warning }
}

function MobileJoinScreen({ roomCode }: MobileJoinScreenProps) {
  const [enteredRoomCode, setEnteredRoomCode] = useState(() =>
    roomCode.trim().toUpperCase(),
  )
  const [roomSnapshotState, setRoomSnapshotState] =
    useState<RoomSnapshotState | null>(null)
  const [playerName, setPlayerName] = useState('')
  const [fieldErrors, setFieldErrors] = useState<MobileJoinFieldErrors>({})
  const [status, setStatus] = useState<MobileJoinStatus>('idle')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [joinedPlayer, setJoinedPlayer] = useState<JoinedPlayerSummary | null>(null)
  const [realtimeStatus, setRealtimeStatus] =
    useState<MobileRoomRealtimeStatus | null>(null)
  const [hasRealtimeError, setHasRealtimeError] = useState(false)
  const [realtimeRetryVersion, setRealtimeRetryVersion] = useState(0)
  const roomCodeInputRef = useRef<HTMLInputElement>(null)
  const playerNameInputRef = useRef<HTMLInputElement>(null)
  const joinRequestInFlightRef = useRef(false)
  const joinAbortControllerRef = useRef<AbortController | null>(null)
  const isBusy = status === 'recovering' || status === 'joining'
  const normalizedEnteredRoomCode = enteredRoomCode.trim().toUpperCase()
  const hasValidEnteredRoomCode = ROOM_CODE_PATTERN.test(normalizedEnteredRoomCode)
  const visibleRoomSnapshotState = hasValidEnteredRoomCode
    && roomSnapshotState?.roomCode === normalizedEnteredRoomCode
    ? roomSnapshotState
    : null
  const isLoadingRoomSnapshot = hasValidEnteredRoomCode
    && visibleRoomSnapshotState === null

  useEffect(() => {
    if (!ROOM_CODE_PATTERN.test(normalizedEnteredRoomCode)) {
      return
    }

    const abortController = new AbortController()
    let isCurrent = true

    async function loadRoomSnapshot() {
      try {
        const snapshot = await getRoomState(
          normalizedEnteredRoomCode,
          abortController.signal,
        )

        if (!isCurrent) {
          return
        }

        setRoomSnapshotState({
          roomCode: normalizedEnteredRoomCode,
          status: 'loaded',
          snapshot,
        })
      } catch (error) {
        if (!isCurrent || isAbortError(error)) {
          return
        }

        setRoomSnapshotState({
          roomCode: normalizedEnteredRoomCode,
          status: 'error',
          message: getRoomSnapshotErrorMessage(error),
        })
      }
    }

    void loadRoomSnapshot()

    return () => {
      isCurrent = false
      abortController.abort()
    }
  }, [normalizedEnteredRoomCode])

  useEffect(() => {
    const normalizedRoomCode = roomCode.trim().toUpperCase()
    if (!ROOM_CODE_PATTERN.test(normalizedRoomCode)) {
      return
    }

    const abortController = new AbortController()
    let isCurrent = true

    async function recoverSessionFromLink() {
      try {
        const session = readMobilePlayerSession(normalizedRoomCode)
        if (!session || !isCurrent) {
          return
        }

        setEnteredRoomCode(normalizedRoomCode)
        setPlayerName(session.playerName)
        setErrorMessage(null)
        setStatus('recovering')

        try {
          const privateState = await getPlayerPrivateState(
            session.roomCode,
            session.playerId,
            abortController.signal,
          )
          if (!isCurrent) {
            return
          }

          const savedSession = persistJoinedSession(session, {
            roomCode: privateState.room.roomCode,
            playerId: privateState.player.playerId,
            playerName: privateState.player.name,
          })
          setJoinedPlayer(savedSession.player)
          setErrorMessage(savedSession.warning)
          setStatus('joined')
        } catch (error) {
          if (!isCurrent || isAbortError(error)) {
            return
          }

          if (!isNotFoundError(error)) {
            setErrorMessage(getJoinErrorMessage(error, 'recovery'))
            return
          }

          if (session.status === 'joined') {
            clearMobilePlayerSession(session.roomCode)
            return
          }

          setStatus('joining')
          try {
            const player = await requestPlayerJoin(session, abortController.signal)
            if (!isCurrent) {
              return
            }

            const savedSession = persistJoinedSession(session, player)
            setJoinedPlayer(savedSession.player)
            setErrorMessage(savedSession.warning)
            setStatus('joined')
          } catch (joinError) {
            if (!isCurrent || isAbortError(joinError)) {
              return
            }

            setErrorMessage(getJoinErrorMessage(joinError, 'join'))
          }
        }
      } catch (error) {
        if (isCurrent) {
          setErrorMessage(getJoinErrorMessage(error, 'recovery'))
        }
      } finally {
        if (isCurrent) {
          setStatus((currentStatus) =>
            currentStatus === 'recovering' || currentStatus === 'joining'
              ? 'idle'
              : currentStatus,
          )
        }
      }
    }

    void recoverSessionFromLink()

    return () => {
      isCurrent = false
      abortController.abort()
    }
  }, [roomCode])

  useEffect(() => () => {
    joinAbortControllerRef.current?.abort()
  }, [])

  useEffect(() => {
    if (!joinedPlayer) {
      return
    }

    let isCurrent = true

    const realtimeClient = createMobileRoomRealtimeClient(
      joinedPlayer.roomCode,
      joinedPlayer.playerId,
      {
        onStatusChanged(nextStatus) {
          if (!isCurrent) {
            return
          }

          setRealtimeStatus(nextStatus)
          if (nextStatus === 'connected') {
            setHasRealtimeError(false)
          }
        },
        onError() {
          if (isCurrent) {
            setHasRealtimeError(true)
          }
        },
      },
    )

    void Promise.resolve()
      .then(() => {
        if (isCurrent) {
          return realtimeClient.start()
        }
      })
      .catch(() => {
        if (isCurrent) {
          setRealtimeStatus('disconnected')
          setHasRealtimeError(true)
        }
      })

    return () => {
      isCurrent = false
      void realtimeClient.stop()
    }
  }, [joinedPlayer, realtimeRetryVersion])

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

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (joinRequestInFlightRef.current || status !== 'idle') {
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
    } else if (normalizedPlayerName.length > 20) {
      nextErrors.playerName = 'El nombre debe tener entre 1 y 20 caracteres.'
    }

    setEnteredRoomCode(normalizedRoomCode)
    setPlayerName(normalizedPlayerName)
    setFieldErrors(nextErrors)
    setErrorMessage(null)

    if (nextErrors.roomCode) {
      roomCodeInputRef.current?.focus()
      return
    }

    if (nextErrors.playerName) {
      playerNameInputRef.current?.focus()
      return
    }

    joinRequestInFlightRef.current = true
    const abortController = new AbortController()
    joinAbortControllerRef.current = abortController
    setStatus('joining')

    let operation: JoinOperation = 'recovery'
    try {
      let session = readMobilePlayerSession(normalizedRoomCode)

      if (session) {
        try {
          const privateState = await getPlayerPrivateState(
            session.roomCode,
            session.playerId,
            abortController.signal,
          )
          if (abortController.signal.aborted) {
            return
          }

          const savedSession = persistJoinedSession(session, {
            roomCode: privateState.room.roomCode,
            playerId: privateState.player.playerId,
            playerName: privateState.player.name,
          })
          setJoinedPlayer(savedSession.player)
          setErrorMessage(savedSession.warning)
          setStatus('joined')
          return
        } catch (error) {
          if (abortController.signal.aborted || isAbortError(error)) {
            return
          }

          if (!isNotFoundError(error)) {
            throw error
          }

          if (session.status === 'joined') {
            clearMobilePlayerSession(session.roomCode)
            session = null
          } else {
            session = {
              ...session,
              playerName: normalizedPlayerName,
            }
            writeMobilePlayerSession(session)
          }
        }
      }

      if (!session) {
        session = {
          roomCode: normalizedRoomCode,
          playerId: createMobilePlayerId(),
          playerName: normalizedPlayerName,
          status: 'joining',
        }
        // Save the stable ID before POST so an uncertain response can be recovered.
        writeMobilePlayerSession(session)
      }

      operation = 'join'
      const player = await requestPlayerJoin(session, abortController.signal)
      if (abortController.signal.aborted) {
        return
      }

      const savedSession = persistJoinedSession(session, player)
      setJoinedPlayer(savedSession.player)
      setErrorMessage(savedSession.warning)
      setStatus('joined')
    } catch (error) {
      if (!abortController.signal.aborted && !isAbortError(error)) {
        setErrorMessage(getJoinErrorMessage(error, operation))
        setStatus('idle')
      }
    } finally {
      joinRequestInFlightRef.current = false
      if (joinAbortControllerRef.current === abortController) {
        joinAbortControllerRef.current = null
      }
      if (!abortController.signal.aborted) {
        setStatus((currentStatus) =>
          currentStatus === 'joining' ? 'idle' : currentStatus,
        )
      }
    }
  }

  const realtimeStatusMessage = getRealtimeStatusMessage(
    realtimeStatus,
    hasRealtimeError,
  )

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
          {joinedPlayer ? (
            <div className="mobile-join__confirmation">
              <header className="mobile-join__header">
                <p className="mobile-join__eyebrow">
                  Sala {joinedPlayer.roomCode}
                </p>
                <h1 className="mobile-join__title" id="mobile-join-title">
                  {joinedPlayer.playerName}
                </h1>
              </header>
              <p
                className="mobile-join__joined-status"
                role="status"
                aria-live="polite"
              >
                Esperando a que la TV inicie la partida
              </p>
              {realtimeStatusMessage && (
                <p
                  className="mobile-join__realtime-status"
                  role="status"
                  aria-live="polite"
                >
                  {realtimeStatusMessage}
                </p>
              )}
              {(hasRealtimeError || realtimeStatus === 'disconnected') && (
                <button
                  className="mobile-join__submit"
                  onClick={() => {
                    setRealtimeStatus('connecting')
                    setHasRealtimeError(false)
                    setRealtimeRetryVersion((version) => version + 1)
                  }}
                  type="button"
                >
                  Reintentar conexión
                </button>
              )}
              {errorMessage && (
                <p className="mobile-join__form-error" role="alert">
                  {errorMessage}
                </p>
              )}
            </div>
          ) : (
            <>
              <header className="mobile-join__header">
                <p className="mobile-join__eyebrow">
                  ImaginArte · Control móvil
                </p>
                <h1 className="mobile-join__title" id="mobile-join-title">
                  Unirse a sala
                </h1>
                <p className="mobile-join__introduction">
                  Escribe el código que aparece en la televisión y el nombre
                  con el que te reconocerán.
                </p>
              </header>

              <form
                aria-busy={isBusy}
                className="mobile-join__form"
                noValidate
                onSubmit={handleSubmit}
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
                    disabled={isBusy}
                    id="mobile-join-room-code"
                    name="roomCode"
                    onChange={(event) => {
                      const nextRoomCode = event.currentTarget.value
                      const nextNormalizedRoomCode = nextRoomCode.trim().toUpperCase()
                      setEnteredRoomCode(nextRoomCode)
                      if (nextNormalizedRoomCode !== normalizedEnteredRoomCode) {
                        setRoomSnapshotState(null)
                      }
                      clearFieldError('roomCode')
                      setErrorMessage(null)
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
                    disabled={isBusy}
                    id="mobile-join-player-name"
                    maxLength={20}
                    name="playerName"
                    onChange={(event) => {
                      setPlayerName(event.currentTarget.value)
                      clearFieldError('playerName')
                      setErrorMessage(null)
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

                {isLoadingRoomSnapshot && (
                  <p
                    className="mobile-join__status"
                    role="status"
                    aria-live="polite"
                  >
                    Consultando la sala…
                  </p>
                )}

                {visibleRoomSnapshotState?.status === 'error' && (
                  <p className="mobile-join__form-error" role="alert">
                    {visibleRoomSnapshotState.message}
                  </p>
                )}

                {isBusy && (
                  <p
                    className="mobile-join__status"
                    role="status"
                    aria-live="polite"
                  >
                    {status === 'recovering'
                      ? 'Recuperando tu sesión…'
                      : 'Uniéndote a la sala…'}
                  </p>
                )}

                {errorMessage && (
                  <p className="mobile-join__form-error" role="alert">
                    {errorMessage}
                  </p>
                )}

                <button
                  aria-busy={isBusy}
                  className="mobile-join__submit"
                  disabled={isBusy}
                  type="submit"
                >
                  {isBusy
                    ? status === 'recovering'
                      ? 'Recuperando…'
                      : 'Uniéndote…'
                    : errorMessage
                      ? 'Reintentar'
                      : 'Unirme a la sala'}
                </button>
              </form>
            </>
          )}

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
