import type { RoomStateResponse } from '../../models/roomState'
import CeremonialWindowDecorations from '../../components/CeremonialWindowDecorations'
import './TvWaitingForStorytellerScreen.css'

interface TvWaitingForStorytellerScreenProps {
  room: RoomStateResponse
}

function TvWaitingForStorytellerScreen({
  room,
}: TvWaitingForStorytellerScreenProps) {
  const storytellerName = room.currentStoryteller?.name ?? 'Narrador'

  return (
    <section
      className="tv-lobby-panel tv-lobby-panel--waiting"
      aria-labelledby="tv-waiting-storyteller-name"
    >
      <CeremonialWindowDecorations />
      <p className="tv-lobby-screen__eyebrow">Es el turno del narrador</p>
      <h2
        className="tv-lobby-screen__waiting-name"
        id="tv-waiting-storyteller-name"
      >
        {storytellerName}
      </h2>
      <p
        className="tv-lobby-screen__waiting-message"
        role="status"
        aria-live="polite"
      >
        Está eligiendo una carta y una pista.
      </p>
    </section>
  )
}

export default TvWaitingForStorytellerScreen
