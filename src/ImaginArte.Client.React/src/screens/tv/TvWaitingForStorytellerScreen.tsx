import type { RoomStateResponse } from '../../models/roomState'
import { getPlayerAvatar } from '../../models/playerAvatars'
import CeremonialWindowDecorations from '../../components/CeremonialWindowDecorations'
import './TvWaitingForStorytellerScreen.css'

interface TvWaitingForStorytellerScreenProps {
  room: RoomStateResponse
}

function TvWaitingForStorytellerScreen({
  room,
}: TvWaitingForStorytellerScreenProps) {
  const storyteller = room.currentStoryteller
  const storytellerName = storyteller?.name ?? 'Narrador'
  const storytellerAvatar = storyteller
    ? getPlayerAvatar(storyteller.avatarId)
    : null
  const isReconnecting = storyteller?.isConnected === false

  return (
    <section
      className="tv-lobby-panel tv-lobby-panel--waiting"
      aria-labelledby="tv-waiting-storyteller-title"
    >
      <CeremonialWindowDecorations />
      <div className="tv-waiting-storyteller__content">
        <h2
          className="tv-waiting-storyteller__title"
          id="tv-waiting-storyteller-title"
        >
          Esperando al narrador
        </h2>

        <div className="tv-waiting-storyteller__identity">
          {storytellerAvatar && (
            <span
              className="tv-lobby-screen__avatar tv-waiting-storyteller__avatar"
              style={getStorytellerAvatarStyle(storytellerAvatar.atlasCrop)}
              aria-hidden="true"
            />
          )}
          <div className="tv-waiting-storyteller__identity-copy">
            <h3 className="tv-lobby-screen__waiting-name">
              {storytellerName}
            </h3>
            <p className="tv-waiting-storyteller__role">NARRADOR</p>
          </div>
        </div>

        <div
          className="tv-waiting-storyteller__messages"
          role="status"
          aria-live="polite"
        >
          <p className="tv-lobby-screen__waiting-message">
            {isReconnecting
              ? 'El narrador se está reconectando…'
              : 'Está eligiendo una carta y una pista.'}
          </p>
          {!isReconnecting && (
            <p className="tv-waiting-storyteller__secondary-message">
              En unos instantes, comienza la magia
            </p>
          )}
        </div>
      </div>
    </section>
  )
}

function getStorytellerAvatarStyle(crop: { column: number; row: number }) {
  return {
    backgroundPosition: `${(crop.column / 3) * 95 + 2}% ${(crop.row / 3) * 95 + 3}%`,
  }
}

export default TvWaitingForStorytellerScreen
