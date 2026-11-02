import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import {
  isPlayerStateUpdatedEvent,
  isRoomUpdatedEvent,
  type GetPlayerPrivateStateResponse,
} from '../models/roomState'
import { getPlayerPrivateState, getRoomsApiBaseUrl } from './roomsApi'

const ROOM_UPDATED_EVENT = 'RoomUpdated'
const PLAYER_STATE_UPDATED_EVENT = 'PlayerStateUpdated'

export type MobileRoomRealtimeStatus =
  | 'connecting'
  | 'connected'
  | 'reconnecting'
  | 'disconnected'

export interface MobileRoomRealtimeCallbacks {
  onStatusChanged: (status: MobileRoomRealtimeStatus) => void
  onPlayerStateUpdated: (state: GetPlayerPrivateStateResponse) => void
  onError: (error: unknown) => void
}

export interface MobileRoomRealtimeClient {
  start: () => Promise<void>
  stop: () => Promise<void>
}

export function createMobileRoomRealtimeClient(
  roomCode: string,
  playerId: string,
  callbacks: MobileRoomRealtimeCallbacks,
): MobileRoomRealtimeClient {
  const normalizedRoomCode = roomCode.trim().toUpperCase()
  const normalizedPlayerId = playerId.toLowerCase()
  const abortController = new AbortController()
  let privateStateVersion = 0
  let isStopped = false
  const connection = new HubConnectionBuilder()
    .withUrl(`${getRoomsApiBaseUrl()}/hubs/rooms`)
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build()

  connection.on(ROOM_UPDATED_EVENT, (event: unknown) => {
    if (
      !isRoomUpdatedEvent(event)
      || event.room.roomCode.trim().toUpperCase() !== normalizedRoomCode
    ) {
      callbacks.onError(new Error('SignalR devolvió una actualización de sala no válida.'))
    }

    // El snapshot público se valida y se descarta: esta pantalla solo confirma la unión.
  })

  connection.on(PLAYER_STATE_UPDATED_EVENT, (event: unknown) => {
    if (
      !isPlayerStateUpdatedEvent(event)
      || event.state.room.roomCode.trim().toUpperCase() !== normalizedRoomCode
      || event.state.player.playerId.toLowerCase() !== normalizedPlayerId
    ) {
      callbacks.onError(new Error('SignalR devolvió un estado privado no válido.'))
      return
    }

    privateStateVersion += 1
    callbacks.onPlayerStateUpdated(event.state)
  })

  connection.onreconnecting(() => {
    callbacks.onStatusChanged('reconnecting')
  })

  connection.onreconnected(() => {
    callbacks.onStatusChanged('reconnecting')
    void resubscribeAfterReconnect()
  })

  connection.onclose((error) => {
    callbacks.onStatusChanged('disconnected')

    if (error) {
      callbacks.onError(error)
    }
  })

  async function subscribeAsPlayer(): Promise<void> {
    await connection.invoke('SubscribeAsPlayer', normalizedRoomCode, playerId)
    const versionBeforeSnapshot = privateStateVersion
    const snapshot = await getPlayerPrivateState(
      normalizedRoomCode,
      playerId,
      abortController.signal,
    )

    if (!isStopped && versionBeforeSnapshot === privateStateVersion) {
      callbacks.onPlayerStateUpdated(snapshot)
    }
  }

  async function resubscribeAfterReconnect(): Promise<void> {
    try {
      // El hub devuelve snapshots nuevos al completar esta suscripción.
      await subscribeAsPlayer()
      callbacks.onStatusChanged('connected')
    } catch (error) {
      if (!isStopped) {
        callbacks.onStatusChanged('disconnected')
        callbacks.onError(error)
      }
    }
  }

  return {
    async start() {
      callbacks.onStatusChanged('connecting')

      try {
        await connection.start()
        await subscribeAsPlayer()
        callbacks.onStatusChanged('connected')
      } catch (error) {
        await connection.stop().catch(() => undefined)
        throw error
      }
    },
    stop() {
      isStopped = true
      abortController.abort()
      return connection.stop()
    },
  }
}
