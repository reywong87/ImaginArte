const CARD_MANIFEST_URL = `${import.meta.env.BASE_URL}cards/manifest.v1.json`
const UNAVAILABLE_ILLUSTRATION_MESSAGE = 'Ilustración no disponible'

interface CardManifestEntry {
  cardId: string
  fileName: string
  alt: string
}

interface CardManifest {
  schemaVersion: 1
  cards: CardManifestEntry[]
}

export interface CardAssetResolution {
  cardId: string
  imageUrl: string | null
  alt: string
  isAvailable: boolean
  isSelectable: boolean
  availabilityMessage: string | null
}

let cardManifestPromise: Promise<CardManifest> | undefined

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}

function isCardManifestEntry(value: unknown): value is CardManifestEntry {
  return isRecord(value)
    && typeof value.cardId === 'string'
    && typeof value.fileName === 'string'
    && typeof value.alt === 'string'
}

function isCardManifest(value: unknown): value is CardManifest {
  return isRecord(value)
    && value.schemaVersion === 1
    && Array.isArray(value.cards)
    && value.cards.every(isCardManifestEntry)
}

async function loadCardManifest(): Promise<CardManifest> {
  const request = cardManifestPromise ??= fetch(CARD_MANIFEST_URL).then(async (response) => {
    if (!response.ok) {
      throw new Error(`No se pudo cargar el manifiesto de cartas: HTTP ${response.status}.`)
    }

    const payload: unknown = await response.json()

    if (!isCardManifest(payload)) {
      throw new Error('El manifiesto de cartas no tiene un formato compatible.')
    }

    return payload
  })

  try {
    return await request
  } catch (error) {
    if (cardManifestPromise === request) {
      cardManifestPromise = undefined
    }

    throw error
  }
}

function unavailableCardAsset(
  cardId: string,
  alt = UNAVAILABLE_ILLUSTRATION_MESSAGE,
): CardAssetResolution {
  return {
    cardId,
    imageUrl: null,
    alt,
    isAvailable: false,
    isSelectable: false,
    availabilityMessage: UNAVAILABLE_ILLUSTRATION_MESSAGE,
  }
}

function getLocalCardAssetUrl(fileName: string): string | null {
  if (!import.meta.env.DEV) {
    return null
  }

  const baseUrl = import.meta.env.BASE_URL.endsWith('/')
    ? import.meta.env.BASE_URL
    : `${import.meta.env.BASE_URL}/`

  return `${baseUrl}__local-cards/${encodeURIComponent(fileName)}`
}

export async function resolveCardAsset(cardId: string): Promise<CardAssetResolution> {
  let card: CardManifestEntry | undefined

  try {
    card = (await loadCardManifest()).cards.find((entry) => entry.cardId === cardId)
  } catch {
    return unavailableCardAsset(cardId)
  }

  if (!card) {
    return unavailableCardAsset(cardId)
  }

  const imageUrl = getLocalCardAssetUrl(card.fileName)

  if (!imageUrl) {
    return unavailableCardAsset(cardId, card.alt)
  }

  try {
    const response = await fetch(imageUrl, { method: 'HEAD' })

    if (!response.ok) {
      return unavailableCardAsset(cardId, card.alt)
    }

    return {
      cardId,
      imageUrl,
      alt: card.alt,
      isAvailable: true,
      isSelectable: true,
      availabilityMessage: null,
    }
  } catch {
    return unavailableCardAsset(cardId, card.alt)
  }
}
