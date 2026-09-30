import type { PlayerStateResponse } from '../models/roomState'

const AVATAR_COUNT = 16
const STORAGE_KEY_PREFIX = 'imaginarte.tv.roomAvatars.'
const inMemoryAssignments = new Map<string, Record<string, number>>()

export function assignPlayerAvatars(
  roomCode: string,
  players: readonly PlayerStateResponse[],
): Record<string, number> {
  const playerIds = [...new Set(players.map((player) => player.playerId))]
  const previousAssignments = readAssignments(roomCode)
  const assignments: Record<string, number> = {}
  const usedAvatarIndexes = new Set<number>()

  for (const playerId of playerIds) {
    const avatarIndex = previousAssignments[playerId]

    if (
      Number.isInteger(avatarIndex)
      && avatarIndex >= 0
      && avatarIndex < AVATAR_COUNT
      && !usedAvatarIndexes.has(avatarIndex)
    ) {
      assignments[playerId] = avatarIndex
      usedAvatarIndexes.add(avatarIndex)
    }
  }

  const remainingAvatarIndexes = shuffle(
    Array.from({ length: AVATAR_COUNT }, (_, index) => index)
      .filter((index) => !usedAvatarIndexes.has(index)),
  )

  for (const playerId of playerIds) {
    if (Object.hasOwn(assignments, playerId)) {
      continue
    }

    const avatarIndex = remainingAvatarIndexes.pop()

    if (avatarIndex === undefined) {
      break
    }

    assignments[playerId] = avatarIndex
  }

  inMemoryAssignments.set(roomCode, assignments)

  try {
    window.sessionStorage.setItem(
      `${STORAGE_KEY_PREFIX}${roomCode}`,
      JSON.stringify(assignments),
    )
  } catch {
    // Keep the assignments stable for this page session if storage is unavailable.
  }

  return assignments
}

function readAssignments(roomCode: string): Record<string, number> {
  const cachedAssignments = inMemoryAssignments.get(roomCode)
  if (cachedAssignments) {
    return cachedAssignments
  }

  try {
    const storedAssignments = window.sessionStorage.getItem(
      `${STORAGE_KEY_PREFIX}${roomCode}`,
    )

    if (!storedAssignments) {
      return {}
    }

    const parsedAssignments: unknown = JSON.parse(storedAssignments)
    if (typeof parsedAssignments !== 'object' || parsedAssignments === null) {
      return {}
    }

    return Object.fromEntries(
      Object.entries(parsedAssignments).filter((entry): entry is [string, number] =>
        typeof entry[1] === 'number'
        && Number.isInteger(entry[1])
        && entry[1] >= 0
        && entry[1] < AVATAR_COUNT,
      ),
    )
  } catch {
    return {}
  }
}

function shuffle(values: number[]): number[] {
  for (let index = values.length - 1; index > 0; index -= 1) {
    const otherIndex = Math.floor(Math.random() * (index + 1))
    const currentValue = values[index]
    values[index] = values[otherIndex]
    values[otherIndex] = currentValue
  }

  return values
}
