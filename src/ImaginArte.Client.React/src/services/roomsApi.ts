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

function getApiBaseUrl(): string {
  const apiBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim().replace(/\/+$/, '')

  if (!apiBaseUrl) {
    throw new RoomsApiConfigurationError()
  }

  return apiBaseUrl
}

export async function createRoom(roomCode: string): Promise<CreatedRoom> {
  const response = await fetch(`${getApiBaseUrl()}/rooms`, {
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
