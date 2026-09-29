import {
  isRoomStateResponse,
  type RoomStateResponse,
} from '../models/roomState'

interface CreatedRoom {
  roomCode: string
}

export class RoomApiError extends Error {
  readonly status: number

  constructor(status: number) {
    super(`La API respondió con HTTP ${status}.`)
    this.name = 'RoomApiError'
    this.status = status
  }
}

export class RoomsApiConfigurationError extends Error {
  constructor() {
    super('Falta configurar VITE_API_BASE_URL.')
    this.name = 'RoomsApiConfigurationError'
  }
}

export function getRoomsApiBaseUrl(): string {
  const apiBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim().replace(/\/+$/, '')

  if (!apiBaseUrl) {
    throw new RoomsApiConfigurationError()
  }

  return apiBaseUrl
}

export async function createRoom(roomCode: string): Promise<CreatedRoom> {
  const response = await fetch(`${getRoomsApiBaseUrl()}/rooms`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ roomCode }),
  })

  if (!response.ok) {
    throw new RoomApiError(response.status)
  }

  const payload: unknown = await response.json()

  if (
    typeof payload !== 'object' ||
    payload === null ||
    !('roomCode' in payload) ||
    typeof payload.roomCode !== 'string' ||
    payload.roomCode.length === 0
  ) {
    throw new Error('El servidor devolvió un código de sala no válido.')
  }

  return { roomCode: payload.roomCode }
}

export async function getRoomState(
  roomCode: string,
  signal?: AbortSignal,
): Promise<RoomStateResponse> {
  const response = await fetch(
    `${getRoomsApiBaseUrl()}/rooms/${encodeURIComponent(roomCode)}`,
    { signal },
  )

  if (!response.ok) {
    throw new RoomApiError(response.status)
  }

  const payload: unknown = await response.json()

  if (!isRoomStateResponse(payload)) {
    throw new Error('El servidor devolvió un snapshot de sala no válido.')
  }

  return payload
}

export async function updateRoomTargetScore(
  roomCode: string,
  targetScore: number,
): Promise<RoomStateResponse> {
  const response = await fetch(
    `${getRoomsApiBaseUrl()}/rooms/${encodeURIComponent(roomCode)}/target-score`,
    {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ targetScore }),
    },
  )

  if (!response.ok) {
    throw new RoomApiError(response.status)
  }

  const payload: unknown = await response.json()

  if (!isRoomStateResponse(payload)) {
    throw new Error('El servidor devolvió un snapshot de sala no válido.')
  }

  return payload
}

export async function checkRoomExists(
  roomCode: string,
  signal: AbortSignal,
): Promise<boolean> {
  const response = await fetch(
    `${getRoomsApiBaseUrl()}/rooms/${encodeURIComponent(roomCode)}`,
    { signal },
  )

  if (response.status === 404) {
    return false
  }

  if (!response.ok) {
    throw new RoomApiError(response.status)
  }

  return true
}
