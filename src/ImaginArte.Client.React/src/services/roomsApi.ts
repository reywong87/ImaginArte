import {
  isGetPlayerPrivateStateResponse,
  isRoomStateResponse,
  type GetPlayerPrivateStateResponse,
  type RoomStateResponse,
} from '../models/roomState'
import type { AvatarId } from '../models/playerAvatars'

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

export async function joinRoomAsPlayer(
  roomCode: string,
  playerId: string,
  playerName: string,
  avatarId: AvatarId,
  signal?: AbortSignal,
): Promise<RoomStateResponse> {
  const normalizedRoomCode = roomCode.trim().toUpperCase()
  const response = await fetch(
    `${getRoomsApiBaseUrl()}/rooms/${encodeURIComponent(normalizedRoomCode)}/players`,
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ playerId, playerName, avatarId }),
      signal,
    },
  )

  if (!response.ok) {
    throw new RoomApiError(response.status)
  }

  const payload: unknown = await response.json()

  if (!isRoomStateResponse(payload)) {
    throw new Error('El servidor devolvió un estado de sala no válido al unirse.')
  }

  return payload
}

export async function getPlayerPrivateState(
  roomCode: string,
  playerId: string,
  signal?: AbortSignal,
): Promise<GetPlayerPrivateStateResponse> {
  const normalizedRoomCode = roomCode.trim().toUpperCase()
  const response = await fetch(
    `${getRoomsApiBaseUrl()}/rooms/${encodeURIComponent(normalizedRoomCode)}/players/${encodeURIComponent(playerId)}`,
    { signal },
  )

  if (!response.ok) {
    throw new RoomApiError(response.status)
  }

  const payload: unknown = await response.json()

  if (!isGetPlayerPrivateStateResponse(payload)) {
    throw new Error('El servidor devolvió una sesión de jugador no válida.')
  }

  if (
    payload.room.roomCode.trim().toUpperCase() !== normalizedRoomCode ||
    payload.player.playerId.toLowerCase() !== playerId.toLowerCase()
  ) {
    throw new Error('El servidor devolvió una sesión de otro jugador o sala.')
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

export async function startRoom(roomCode: string): Promise<RoomStateResponse> {
  const response = await fetch(
    `${getRoomsApiBaseUrl()}/rooms/${encodeURIComponent(roomCode)}/start`,
    { method: 'POST' },
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
