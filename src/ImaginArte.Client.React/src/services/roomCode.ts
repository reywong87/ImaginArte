const ROOM_CODE_CHARACTERS = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789'
const ROOM_CODE_LENGTH = 6

export function createRoomCode(): string {
  const randomBytes = new Uint8Array(ROOM_CODE_LENGTH)
  globalThis.crypto.getRandomValues(randomBytes)

  return Array.from(randomBytes, (byte) =>
    ROOM_CODE_CHARACTERS.charAt(byte % ROOM_CODE_CHARACTERS.length),
  ).join('')
}
