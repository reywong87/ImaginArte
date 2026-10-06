import { isAvatarId, type AvatarId } from '../models/playerAvatars'

export interface StoredMobilePlayerSession {
  roomCode: string
  playerId: string
  playerName: string
  avatarId: AvatarId
  status: 'joining' | 'joined'
}

const SESSION_KEY_PREFIX = 'imaginarte.mobile.v2.room.'
const LEGACY_SESSION_KEY_PREFIX = 'imaginarte.mobile.v1.room.'
const PLAYER_ID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

export class MobilePlayerSessionStorageError extends Error {
  constructor() {
    super(
      'Este navegador no permite guardar la sesión. Activa el almacenamiento local e inténtalo de nuevo.',
    )
    this.name = 'MobilePlayerSessionStorageError'
  }
}

function normalizeRoomCode(roomCode: string): string {
  return roomCode.trim().toUpperCase()
}

function getSessionKey(roomCode: string): string {
  return `${SESSION_KEY_PREFIX}${normalizeRoomCode(roomCode)}.player`
}

function getLegacySessionKey(roomCode: string): string {
  return `${LEGACY_SESSION_KEY_PREFIX}${normalizeRoomCode(roomCode)}.player`
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}

function isStoredMobilePlayerSession(
  value: unknown,
  normalizedRoomCode: string,
): value is StoredMobilePlayerSession {
  return isRecord(value)
    && typeof value.roomCode === 'string'
    && normalizeRoomCode(value.roomCode) === normalizedRoomCode
    && typeof value.playerId === 'string'
    && PLAYER_ID_PATTERN.test(value.playerId)
    && typeof value.playerName === 'string'
    && value.playerName.trim().length >= 1
    && value.playerName.trim().length <= 40
    && isAvatarId(value.avatarId)
    && (value.status === 'joining' || value.status === 'joined')
}

function removeStoredSession(key: string): void {
  try {
    window.localStorage.removeItem(key)
  } catch {
    throw new MobilePlayerSessionStorageError()
  }
}

export function readMobilePlayerSession(
  roomCode: string,
): StoredMobilePlayerSession | null {
  const normalizedRoomCode = normalizeRoomCode(roomCode)
  const legacyKey = getLegacySessionKey(normalizedRoomCode)
  const key = getSessionKey(normalizedRoomCode)
  let storedValue: string | null

  try {
    if (window.localStorage.getItem(legacyKey) !== null) {
      // Legacy records have no explicit avatar choice, so never restore or retry them.
      window.localStorage.removeItem(legacyKey)
    }

    storedValue = window.localStorage.getItem(key)
  } catch {
    throw new MobilePlayerSessionStorageError()
  }

  if (storedValue === null) {
    return null
  }

  let parsedValue: unknown
  try {
    parsedValue = JSON.parse(storedValue)
  } catch {
    removeStoredSession(key)
    return null
  }

  if (!isStoredMobilePlayerSession(parsedValue, normalizedRoomCode)) {
    removeStoredSession(key)
    return null
  }

  return {
    roomCode: normalizedRoomCode,
    playerId: parsedValue.playerId,
    playerName: parsedValue.playerName.trim(),
    avatarId: parsedValue.avatarId,
    status: parsedValue.status,
  }
}

export function writeMobilePlayerSession(
  session: StoredMobilePlayerSession,
): void {
  const normalizedSession: StoredMobilePlayerSession = {
    ...session,
    roomCode: normalizeRoomCode(session.roomCode),
    playerName: session.playerName.trim(),
  }

  try {
    window.localStorage.setItem(
      getSessionKey(normalizedSession.roomCode),
      JSON.stringify(normalizedSession),
    )
  } catch {
    throw new MobilePlayerSessionStorageError()
  }
}

export function clearMobilePlayerSession(roomCode: string): void {
  removeStoredSession(getSessionKey(roomCode))
}

export function createMobilePlayerId(): string {
  const randomBytes = new Uint8Array(16)
  globalThis.crypto.getRandomValues(randomBytes)

  randomBytes[6] = (randomBytes[6] & 0x0f) | 0x40
  randomBytes[8] = (randomBytes[8] & 0x3f) | 0x80

  const hex = Array.from(randomBytes, (byte) =>
    byte.toString(16).padStart(2, '0'),
  ).join('')

  return [
    hex.slice(0, 8),
    hex.slice(8, 12),
    hex.slice(12, 16),
    hex.slice(16, 20),
    hex.slice(20),
  ].join('-')
}
