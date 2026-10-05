export const PLAYER_AVATARS = {
  fox: { accessibleName: 'Zorro', atlasCrop: { column: 0, row: 0 } },
  owl: { accessibleName: 'Búho', atlasCrop: { column: 1, row: 0 } },
  deer: { accessibleName: 'Ciervo', atlasCrop: { column: 2, row: 0 } },
  moth: { accessibleName: 'Polilla', atlasCrop: { column: 3, row: 0 } },
  turtle: { accessibleName: 'Tortuga', atlasCrop: { column: 0, row: 1 } },
  octopus: { accessibleName: 'Pulpo', atlasCrop: { column: 1, row: 1 } },
  mushroom: { accessibleName: 'Seta', atlasCrop: { column: 2, row: 1 } },
  dragon: { accessibleName: 'Dragón', atlasCrop: { column: 3, row: 1 } },
  rabbit: { accessibleName: 'Conejo', atlasCrop: { column: 0, row: 2 } },
  raven: { accessibleName: 'Cuervo', atlasCrop: { column: 1, row: 2 } },
  hummingbird: { accessibleName: 'Colibrí', atlasCrop: { column: 2, row: 2 } },
  koi: { accessibleName: 'Pez koi', atlasCrop: { column: 3, row: 2 } },
  lynx: { accessibleName: 'Lince', atlasCrop: { column: 0, row: 3 } },
  swan: { accessibleName: 'Cisne', atlasCrop: { column: 1, row: 3 } },
  phoenix: { accessibleName: 'Fénix', atlasCrop: { column: 2, row: 3 } },
  snail: { accessibleName: 'Caracol', atlasCrop: { column: 3, row: 3 } },
} as const

export type AvatarId = keyof typeof PLAYER_AVATARS

export function isAvatarId(value: unknown): value is AvatarId {
  return typeof value === 'string' && Object.hasOwn(PLAYER_AVATARS, value)
}

export function getPlayerAvatar(avatarId: AvatarId) {
  return PLAYER_AVATARS[avatarId]
}
