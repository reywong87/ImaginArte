import CeremonialWindowDecorations from '../components/CeremonialWindowDecorations'
import './MobileJoinScreen.css'

interface MobileJoinScreenProps {
  roomCode: string
}

function MobileJoinScreen({ roomCode }: MobileJoinScreenProps) {
  return (
    <main className="mobile-join" aria-labelledby="mobile-join-title">
      <section className="mobile-join__panel">
        <CeremonialWindowDecorations />

        <header className="mobile-join__header">
          <p className="mobile-join__eyebrow">ImaginArte · Control móvil</p>
          <h1 className="mobile-join__title" id="mobile-join-title">
            Unirse a sala
          </h1>
          <p className="mobile-join__introduction">
            Escribe el código que aparece en la televisión y el nombre con el
            que te reconocerán.
          </p>
        </header>

        <form
          className="mobile-join__form"
          onSubmit={(event) => event.preventDefault()}
        >
          <div className="mobile-join__field">
            <label htmlFor="mobile-join-room-code">Código de sala</label>
            <input
              autoCapitalize="characters"
              autoComplete="off"
              className="mobile-join__input mobile-join__input--code"
              defaultValue={roomCode}
              id="mobile-join-room-code"
              name="roomCode"
              placeholder="ABC234"
              type="text"
            />
          </div>

          <div className="mobile-join__field">
            <label htmlFor="mobile-join-player-name">Tu nombre</label>
            <input
              autoComplete="name"
              className="mobile-join__input"
              id="mobile-join-player-name"
              name="playerName"
              placeholder="Cómo te llamas"
              type="text"
            />
          </div>

          <button className="mobile-join__submit" type="submit">
            Unirme a la sala
          </button>
        </form>

        <p className="mobile-join__privacy">
          <span aria-hidden="true">✦</span>
          Esta pantalla es privada; tus cartas no se mostrarán aquí en la TV.
        </p>
      </section>
    </main>
  )
}

export default MobileJoinScreen
