import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import CeremonialWindowDecorations from '../../components/CeremonialWindowDecorations'
import {
  getPlayerAvatar,
  PLAYER_AVATARS,
  type AvatarId,
} from '../../models/playerAvatars'
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
import {
  RoomPhase,
  type GetPlayerPrivateStateResponse,
  type RoomStateResponse,
} from '../../models/roomState'
import {
  createMobileRoomRealtimeClient,
  type MobileRoomRealtimeStatus,
} from '../../services/mobileRoomRealtime'
import './MobileJoinScreen.css'
import MobileNarratorTurnScreen from './MobileNarratorTurnScreen'
import MobilePlayerTurnScreen from './MobilePlayerTurnScreen'

const ROOM_CODE_PATTERN = /^[ABCDEFGHJKLMNPQRSTUVWXYZ23456789]{6}$/
const MOBILE_AVATAR_IDS = Object.keys(PLAYER_AVATARS) as AvatarId[]

interface MobileJoinScreenProps {
  roomCode: string
}

type MobileJoinField = 'roomCode' | 'playerName' | 'avatarId'

interface MobileJoinFieldErrors {
  roomCode?: string
  playerName?: string
  avatarId?: string
}

type MobileJoinStatus = 'idle' | 'recovering' | 'joining' | 'joined'
type JoinOperation = 'recovery' | 'join'

const AVATAR_CONFLICT_MESSAGE =
  'Ese avatar acaba de ocuparse. Elige otro disponible; conservamos el código y tu nombre.'

type RoomSnapshotState =
  | { roomCode: string; status: 'loaded'; snapshot: RoomStateResponse }
  | { roomCode: string; status: 'error'; message: string }

interface JoinedPlayerSummary {
  roomCode: string
  playerId: string
  playerName: string
  avatarId: AvatarId
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

function isAvatarConflictError(error: unknown): error is RoomApiError {
  return error instanceof RoomApiError && error.status === 409
}

function isAbortError(error: unknown): boolean {
  return error instanceof Error && error.name === 'AbortError'
}

function getJoinedPlayerSummary(
  room: RoomStateResponse,
  playerId: string,
): JoinedPlayerSummary {
  const player = room.players.find(
    (candidate) => candidate.playerId.toLowerCase() === playerId.toLowerCase(),
  )

  if (!player) {
    throw new Error('El snapshot no confirmó la identidad del jugador unido.')
  }

  return {
    roomCode: room.roomCode,
    playerId: player.playerId,
    playerName: player.name,
    avatarId: player.avatarId,
  }
}

function discardPendingJoinAfterAvatarConflict(roomCode: string): boolean {
  try {
    // A 409 confirms that this pending identity was not joined; discard it before choosing again.
    clearMobilePlayerSession(roomCode)
    return true
  } catch {
    return false
  }
}

function getAvatarCropStyle(avatarId: AvatarId) {
  const { column, row } = getPlayerAvatar(avatarId).atlasCrop

  return {
    backgroundPosition: `${(column / 3) * 95 + 2}% ${(row / 3) * 95 + 3 }%`,
  }
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
      return AVATAR_CONFLICT_MESSAGE
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
    session.avatarId,
    signal,
  )

  return getJoinedPlayerSummary(room, session.playerId)
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
    avatarId: player.avatarId,
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
  const [roomSnapshotRetryVersion, setRoomSnapshotRetryVersion] = useState(0)
  const [playerName, setPlayerName] = useState('')
  const [selectedAvatarId, setSelectedAvatarId] = useState<AvatarId | null>(null)
  const [fieldErrors, setFieldErrors] = useState<MobileJoinFieldErrors>({})
  const [status, setStatus] = useState<MobileJoinStatus>('idle')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [joinedPlayer, setJoinedPlayer] = useState<JoinedPlayerSummary | null>(null)
  const [privateState, setPrivateState] = useState<GetPlayerPrivateStateResponse | null>(null)
  const privateStateRef = useRef<GetPlayerPrivateStateResponse | null>(null)
  const privateStateVersionRef = useRef(0)
  const [realtimeStatus, setRealtimeStatus] =
    useState<MobileRoomRealtimeStatus | null>(null)
  const [hasRealtimeError, setHasRealtimeError] = useState(false)
  const [realtimeRetryVersion, setRealtimeRetryVersion] = useState(0)
  const roomCodeInputRef = useRef<HTMLInputElement>(null)
  const playerNameInputRef = useRef<HTMLInputElement>(null)
  const avatarFieldsetRef = useRef<HTMLFieldSetElement>(null)
  const joinRequestInFlightRef = useRef(false)
  const joinAbortControllerRef = useRef<AbortController | null>(null)
  const isBusy = status === 'recovering' || status === 'joining'
  function updatePrivateState(nextState: GetPlayerPrivateStateResponse) {
    privateStateVersionRef.current += 1
    privateStateRef.current = nextState
    setPrivateState(nextState)
  }
  const normalizedEnteredRoomCode = enteredRoomCode.trim().toUpperCase()
  const hasValidEnteredRoomCode = ROOM_CODE_PATTERN.test(normalizedEnteredRoomCode)
  const visibleRoomSnapshotState = hasValidEnteredRoomCode
    && roomSnapshotState?.roomCode === normalizedEnteredRoomCode
    ? roomSnapshotState
    : null
  const isLoadingRoomSnapshot = hasValidEnteredRoomCode
    && visibleRoomSnapshotState === null
  const currentRoomSnapshot = visibleRoomSnapshotState?.status === 'loaded'
    ? visibleRoomSnapshotState.snapshot
    : null
  const occupiedAvatarIds = new Set(
    currentRoomSnapshot?.players.map((player) => player.avatarId) ?? [],
  )
  const isSelectedAvatarAvailable = selectedAvatarId !== null
    && currentRoomSnapshot !== null
    && !occupiedAvatarIds.has(selectedAvatarId)
  const avatarHelpMessage = currentRoomSnapshot
    ? 'Los avatares elegidos por otras personas aparecen como «No disponible».'
    : isLoadingRoomSnapshot
      ? 'Consultando la disponibilidad antes de que puedas elegir.'
      : visibleRoomSnapshotState?.status === 'error'
        ? 'No es posible elegir hasta consultar la disponibilidad de la sala.'
        : 'Introduce un código válido para consultar los avatares disponibles.'

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
  }, [normalizedEnteredRoomCode, roomSnapshotRetryVersion])

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
        setSelectedAvatarId(session.avatarId)
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

          const savedSession = persistJoinedSession(
            session,
            getJoinedPlayerSummary(privateState.room, session.playerId),
          )
          updatePrivateState(privateState)
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

            if (isAvatarConflictError(joinError)) {
              const sessionCleared = discardPendingJoinAfterAvatarConflict(
                normalizedRoomCode,
              )
              setSelectedAvatarId(null)
              setRoomSnapshotState(null)
              setRoomSnapshotRetryVersion((version) => version + 1)
              setErrorMessage(sessionCleared
                ? AVATAR_CONFLICT_MESSAGE
                : `${AVATAR_CONFLICT_MESSAGE} No pudimos limpiar la sesión pendiente; vuelve a intentarlo.`)
            } else {
              setErrorMessage(getJoinErrorMessage(joinError, 'join'))
            }
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
        onPlayerStateUpdated(nextState) {
          if (isCurrent) {
            updatePrivateState(nextState)
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

    if (
      selectedAvatarId === null
      || currentRoomSnapshot === null
      || occupiedAvatarIds.has(selectedAvatarId)
    ) {
      setFieldErrors({
        ...nextErrors,
        avatarId: 'Elige un avatar disponible antes de unirte a la sala.',
      })
      const firstAvailableAvatar = avatarFieldsetRef.current?.querySelector<HTMLInputElement>(
        'input[type="radio"]:not(:disabled)',
      )
      if (firstAvailableAvatar) {
        firstAvailableAvatar.focus()
      } else {
        avatarFieldsetRef.current?.focus()
      }
      return
    }

    const selectedAvatarForJoin: AvatarId = selectedAvatarId

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

          const savedSession = persistJoinedSession(
            session,
            getJoinedPlayerSummary(privateState.room, session.playerId),
          )
          updatePrivateState(privateState)
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
            setSelectedAvatarId(session.avatarId)
            writeMobilePlayerSession(session)
          }
        }
      }

      if (!session) {
        session = {
          roomCode: normalizedRoomCode,
          playerId: createMobilePlayerId(),
          playerName: normalizedPlayerName,
          avatarId: selectedAvatarForJoin,
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
        if (operation === 'join' && isAvatarConflictError(error)) {
          const sessionCleared = discardPendingJoinAfterAvatarConflict(
            normalizedRoomCode,
          )
          setSelectedAvatarId(null)
          setRoomSnapshotState(null)
          setRoomSnapshotRetryVersion((version) => version + 1)
          setErrorMessage(sessionCleared
            ? AVATAR_CONFLICT_MESSAGE
            : `${AVATAR_CONFLICT_MESSAGE} No pudimos limpiar la sesión pendiente; vuelve a intentarlo.`)
        } else {
          setErrorMessage(getJoinErrorMessage(error, operation))
        }
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
  const showNarratorTurn = joinedPlayer !== null
    && privateState?.player.isCurrentStoryteller === true
    && privateState.room.phase === RoomPhase.StorytellerChoosing
  const showPlayerTurnExample = import.meta.env.DEV
    && joinedPlayer !== null
    && privateState?.player.isCurrentStoryteller === false
    && privateState.room.phase === RoomPhase.GuessersSubmitting
    && privateState.player.hasSubmittedGuessCard === false
  const showTurnScreen = showNarratorTurn || showPlayerTurnExample
  const showNarratorWaiting = joinedPlayer !== null
    && privateState?.player.isCurrentStoryteller === true
    && privateState.room.phase !== RoomPhase.WaitingForPlayers
    && privateState.room.phase !== RoomPhase.StorytellerChoosing
  const showPlayerWaiting = joinedPlayer !== null
    && privateState?.player.isCurrentStoryteller === false
    && privateState.room.phase === RoomPhase.GuessersSubmitting
    && privateState.player.hasSubmittedGuessCard

  async function recoverNarratorState(): Promise<GetPlayerPrivateStateResponse> {
    if (!joinedPlayer) throw new Error('No hay una sesión de jugador activa.')
    const version = privateStateVersionRef.current
    const snapshot = await getPlayerPrivateState(joinedPlayer.roomCode, joinedPlayer.playerId)
    if (version === privateStateVersionRef.current) {
      updatePrivateState(snapshot)
      return snapshot
    }
    return privateStateRef.current ?? snapshot
  }

  return (
    <main className="mobile-join" aria-labelledby="mobile-join-title">
      <div className="mobile-join__backdrop" aria-hidden="true" />

      <div className="mobile-join__frame" aria-hidden="true">
        <CeremonialWindowDecorations />
      </div>

      <div
        className={`mobile-join__scroll-region${showTurnScreen ? ' mobile-join__scroll-region--narrator' : ''}`}
        role="region"
        aria-label="Contenido para unirse a la sala"
        tabIndex={0}
      >
        <section className={`mobile-join__panel${showTurnScreen ? ' mobile-join__panel--narrator' : ''}`}>
          {showNarratorTurn ? (
              <MobileNarratorTurnScreen
                hand={privateState.player.hand}
                roomCode={joinedPlayer.roomCode}
                playerId={joinedPlayer.playerId}
                connected={realtimeStatus === 'connected' && !hasRealtimeError}
                onRetryConnection={hasRealtimeError || realtimeStatus === 'disconnected' ? () => {
                  setRealtimeStatus('connecting')
                  setHasRealtimeError(false)
                  setRealtimeRetryVersion((version) => version + 1)
                } : null}
                onRecover={recoverNarratorState}
                onConfirmed={(room) => {
                  const current = privateStateRef.current
                  if (current && current.room.phase === RoomPhase.StorytellerChoosing) {
                    updatePrivateState({ ...current, room })
                  }
                }}
              />
          ) : showPlayerTurnExample ? (
            <MobilePlayerTurnScreen clue={privateState.room.clue} hand={privateState.player.hand} roomCode={joinedPlayer.roomCode} playerId={joinedPlayer.playerId} connected={realtimeStatus === 'connected' && !hasRealtimeError} onRecover={recoverNarratorState} onRetryConnection={hasRealtimeError || realtimeStatus === 'disconnected' ? () => {
              setRealtimeStatus('connecting')
              setHasRealtimeError(false)
              setRealtimeRetryVersion((version) => version + 1)
            } : null} />
          ) : joinedPlayer ? (
            <div className="mobile-join__confirmation">
              {showPlayerWaiting ? (
                <header className="mobile-join__header" role="status">
                  <h1 className="mobile-join__title" id="mobile-join-title">Carta enviada</h1>
                  <p className="mobile-join__introduction">Esperamos a los demás. No necesitas hacer nada por ahora.</p>
                </header>
              ) : showNarratorWaiting ? (
                <header className="mobile-join__header" role="status">
                  <p className="mobile-join__eyebrow">Enviado</p>
                  <h1 className="mobile-join__title" id="mobile-join-title">Tu carta está en camino</h1>
                  <p className="mobile-join__introduction">Esperamos las demás historias. No necesitas hacer nada más por ahora.</p>
                </header>
              ) : (
                <>
              <header className="mobile-join__header">
                <p className="mobile-join__eyebrow">
                  Sala {joinedPlayer.roomCode}
                </p>
                <h1 className="mobile-join__title" id="mobile-join-title">
                  {joinedPlayer.playerName}
                </h1>
                <div className="mobile-join__confirmed-avatar">
                  <span
                    aria-hidden="true"
                    className="mobile-join__confirmed-avatar-portrait"
                    style={getAvatarCropStyle(joinedPlayer.avatarId)}
                  />
                  <p className="mobile-join__confirmed-avatar-label">
                    Tu avatar: <strong>{getPlayerAvatar(joinedPlayer.avatarId).accessibleName}</strong>
                  </p>
                </div>
              </header>
              <p
                className="mobile-join__joined-status"
                role="status"
                aria-live="polite"
              >
                {privateState && privateState.room.phase !== RoomPhase.WaitingForPlayers
                  ? 'Esperando a que continúe la partida'
                  : 'Esperando a que la TV inicie la partida'}
              </p>
                </>
              )}
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
                aria-busy={isBusy || isLoadingRoomSnapshot}
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
                        setSelectedAvatarId(null)
                        clearFieldError('avatarId')
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

                <section
                  aria-labelledby="mobile-join-avatar-title"
                  className="mobile-join__avatar-section"
                >
                  <fieldset
                    aria-describedby={fieldErrors.avatarId
                      ? 'mobile-join-avatar-help mobile-join-avatar-error'
                      : 'mobile-join-avatar-help'}
                    className="mobile-join__avatar-fieldset"
                    ref={avatarFieldsetRef}
                    tabIndex={-1}
                  >
                    <legend
                      className="mobile-join__avatar-title"
                      id="mobile-join-avatar-title"
                    >
                      Elige tu avatar
                    </legend>

                    <div
                      aria-live="polite"
                      className="mobile-join__avatar-preview"
                    >
                      {selectedAvatarId ? (
                        <span
                          aria-hidden="true"
                          className="mobile-join__avatar-preview-portrait"
                          style={getAvatarCropStyle(selectedAvatarId)}
                        />
                      ) : (
                        <span
                          aria-hidden="true"
                          className="mobile-join__avatar-preview-portrait mobile-join__avatar-preview-portrait--empty"
                        >
                          ✦
                        </span>
                      )}
                      <p className="mobile-join__avatar-preview-label">
                        {selectedAvatarId
                          ? `Avatar elegido: ${getPlayerAvatar(selectedAvatarId).accessibleName}`
                          : 'Aún no has elegido ningún avatar.'}
                      </p>
                    </div>

                    <p
                      aria-live="polite"
                      className="mobile-join__avatar-help"
                      id="mobile-join-avatar-help"
                      role="status"
                    >
                      {avatarHelpMessage}
                    </p>

                    <div className="mobile-join__avatar-grid">
                      {MOBILE_AVATAR_IDS.map((avatarId) => {
                        const avatar = getPlayerAvatar(avatarId)
                        const isSelected = selectedAvatarId === avatarId
                        const isOccupied = occupiedAvatarIds.has(avatarId)
                        const isUnavailable = currentRoomSnapshot === null || isOccupied
                        const optionStatus = isOccupied
                          ? 'No disponible'
                          : currentRoomSnapshot
                            ? isSelected ? 'Elegido' : 'Disponible'
                            : visibleRoomSnapshotState?.status === 'error'
                              ? 'Sin verificar'
                              : isLoadingRoomSnapshot
                                ? 'Consultando'
                                : 'Pendiente'
                        const choiceClassName = [
                          'mobile-join__avatar-choice',
                          isSelected && 'mobile-join__avatar-choice--selected',
                          isOccupied && 'mobile-join__avatar-choice--unavailable',
                          isUnavailable && 'mobile-join__avatar-choice--disabled',
                        ].filter(Boolean).join(' ')

                        return (
                          <label
                            className={choiceClassName}
                            key={avatarId}
                          >
                            <input
                              aria-describedby="mobile-join-avatar-help"
                              checked={isSelected}
                              disabled={isBusy || isUnavailable}
                              name="avatarId"
                              onChange={() => {
                                setSelectedAvatarId(avatarId)
                                clearFieldError('avatarId')
                                setErrorMessage(null)
                              }}
                              type="radio"
                              value={avatarId}
                            />
                            <span
                              aria-hidden="true"
                              className="mobile-join__avatar-choice-portrait"
                              style={getAvatarCropStyle(avatarId)}
                            />
                            <span className="mobile-join__avatar-choice-name">
                              {avatar.accessibleName}
                            </span>
                            <span className="mobile-join__avatar-choice-status">
                              {optionStatus}
                            </span>
                          </label>
                        )
                      })}
                    </div>
                  </fieldset>

                  {fieldErrors.avatarId && (
                    <p
                      className="mobile-join__field-error"
                      id="mobile-join-avatar-error"
                      role="alert"
                    >
                      {fieldErrors.avatarId}
                    </p>
                  )}
                </section>

                {visibleRoomSnapshotState?.status === 'error' && (
                  <>
                    <p className="mobile-join__form-error" role="alert">
                      {visibleRoomSnapshotState.message}
                    </p>
                    <button
                      className="mobile-join__snapshot-retry"
                      disabled={isBusy || isLoadingRoomSnapshot}
                      onClick={() => {
                        setRoomSnapshotState(null)
                        setErrorMessage(null)
                        setRoomSnapshotRetryVersion((version) => version + 1)
                      }}
                      type="button"
                    >
                      Volver a consultar disponibilidad
                    </button>
                  </>
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
                  aria-busy={isBusy || isLoadingRoomSnapshot}
                  className="mobile-join__submit"
                  disabled={isBusy || (hasValidEnteredRoomCode && !isSelectedAvatarAvailable)}
                  type="submit"
                >
                  {isBusy
                    ? status === 'recovering'
                      ? 'Recuperando…'
                      : 'Uniéndote…'
                    : isLoadingRoomSnapshot
                      ? 'Consultando…'
                      : errorMessage
                      ? 'Reintentar'
                      : 'Unirme a la sala'}
                </button>
              </form>
            </>
          )}

          {!showTurnScreen && <p className="mobile-join__privacy">
            <span aria-hidden="true">✦</span>
            Esta pantalla es privada; tus cartas no se mostrarán aquí en la TV.
          </p>}
        </section>
      </div>
    </main>
  )
}

export default MobileJoinScreen
