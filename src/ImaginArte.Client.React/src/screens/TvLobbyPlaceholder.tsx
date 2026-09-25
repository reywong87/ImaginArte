import './TvLobbyPlaceholder.css'

interface TvLobbyPlaceholderProps {
  roomCode: string
}

function TvLobbyPlaceholder({ roomCode }: TvLobbyPlaceholderProps) {
  return (
    <section
      className="tv-lobby-placeholder"
      aria-labelledby="tv-lobby-placeholder-title"
    >
      <div className="tv-lobby-placeholder__panel">
        <h1
          className="tv-lobby-placeholder__title"
          id="tv-lobby-placeholder-title"
        >
          Sala creada
        </h1>
        <p className="tv-lobby-placeholder__label">Código de sala</p>
        <p className="tv-lobby-placeholder__code">{roomCode}</p>
      </div>
    </section>
  )
}

export default TvLobbyPlaceholder
