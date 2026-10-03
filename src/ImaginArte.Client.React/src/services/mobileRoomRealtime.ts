import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import {
  isPlayerStateUpdatedEvent,
  isRoomUpdatedEvent,
} from '../models/roomState'
import { getRoomsApiBaseUrl } from './roomsApi'

const ROOM_UPDATED_EVENT = 'RoomUpdated'
const PLAYER_STATE_UPDATED_EVENT = 'PlayerStateUpdated'

export type MobileRoomRealtimeStatus =
  | 'connecting'
  | 'connected'
  | 'reconnecting'
  | 'disconnected'

export interface MobileRoomRealtimeCallbacks {
  onStatusChanged: (status: MobileRoomRealtimeStatus) => void
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
    }

    // El estado privado se valida, pero nunca se guarda ni se entrega a la interfaz.
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
  }

  async function resubscribeAfterReconnect(): Promise<void> {
    try {
      // El hub devuelve snapshots nuevos al completar esta suscripción.
      await subscribeAsPlayer()
      callbacks.onStatusChanged('connected')
    } catch (error) {
      callbacks.onStatusChanged('disconnected')
      callbacks.onError(error)
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
      return connection.stop()
    },
  }
}
