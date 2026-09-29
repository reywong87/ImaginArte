import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import {
  isRoomUpdatedEvent,
  type RoomStateResponse,
} from '../models/roomState'
import { getRoomState, getRoomsApiBaseUrl } from './roomsApi'

const ROOM_UPDATED_EVENT = 'RoomUpdated'

export type RoomRealtimeStatus =
  | 'connecting'
  | 'connected'
  | 'reconnecting'
  | 'disconnected'

export interface RoomRealtimeCallbacks {
  onRoomUpdated: (room: RoomStateResponse) => void
  onStatusChanged: (status: RoomRealtimeStatus) => void
  onError: (error: unknown) => void
}

export interface RoomRealtimeClient {
  start: () => Promise<void>
  stop: () => Promise<void>
}

export function createRoomRealtimeClient(
  roomCode: string,
  callbacks: RoomRealtimeCallbacks,
): RoomRealtimeClient {
  const connection = new HubConnectionBuilder()
    .withUrl(`${getRoomsApiBaseUrl()}/hubs/rooms`)
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build()

  connection.on(ROOM_UPDATED_EVENT, (event: unknown) => {
    if (!isRoomUpdatedEvent(event)) {
      callbacks.onError(new Error('SignalR devolvió una actualización de sala no válida.'))
      return
    }

    callbacks.onRoomUpdated(event.room)
  })

  connection.onreconnecting(() => {
    callbacks.onStatusChanged('reconnecting')
  })

  connection.onreconnected(() => {
    callbacks.onStatusChanged('reconnecting')
    void recoverAfterReconnect()
  })

  connection.onclose((error) => {
    callbacks.onStatusChanged('disconnected')

    if (error) {
      callbacks.onError(error)
    }
  })

  async function recoverAfterReconnect() {
    try {
      await subscribeAndLoadSnapshot()
      callbacks.onStatusChanged('connected')
    } catch (error) {
      callbacks.onStatusChanged('disconnected')
      callbacks.onError(error)
    }
  }

  async function subscribeAndLoadSnapshot() {
    await connection.invoke('SubscribeToRoom', roomCode)
    const currentRoom = await getRoomState(roomCode)
    callbacks.onRoomUpdated(currentRoom)
  }

  return {
    async start() {
      callbacks.onStatusChanged('connecting')
      try {
        await connection.start()
        await subscribeAndLoadSnapshot()
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
