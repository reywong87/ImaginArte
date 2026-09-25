import './TvHomeScreen.css'
import seedIllustration from '../assets/semilla-circulo-estrellas.png'

function TvHomeScreen() {
  return (
    <section className="tv-home" aria-labelledby="tv-home-title">
      <div className="tv-home__content">
        <h1 className="tv-home__title" id="tv-home-title">
          ImaginArte
        </h1>

        <button
          className="room-action"
          type="button"
          aria-labelledby="create-room-title"
          aria-describedby="create-room-description"
        >
          <span className="room-action__texture" aria-hidden="true" />
          <span
            className="room-action__corner room-action__corner--top-left"
            aria-hidden="true"
          />
          <span
            className="room-action__corner room-action__corner--top-right"
            aria-hidden="true"
          />
          <span
            className="room-action__corner room-action__corner--bottom-left"
            aria-hidden="true"
          />
          <span
            className="room-action__corner room-action__corner--bottom-right"
            aria-hidden="true"
          />

          <img
            className="room-action__illustration"
            src={seedIllustration}
            alt=""
            aria-hidden="true"
          />

          <span className="room-action__copy">
            <span className="room-action__title" id="create-room-title">
              Crear una sala
            </span>
            <span
              className="room-action__description"
              id="create-room-description"
            >
              Prepara una nueva experiencia
            </span>
          </span>
        </button>
      </div>
    </section>
  )
}

export default TvHomeScreen
