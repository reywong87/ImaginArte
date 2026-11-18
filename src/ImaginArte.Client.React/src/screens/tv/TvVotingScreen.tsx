import type { RoomStateResponse } from '../../models/roomState'
import './TvVotingScreen.css'

interface TvVotingScreenProps {
  room: Pick<RoomStateResponse, 'clue' | 'voteCount' | 'expectedVoteCount' | 'votingCards'>
}

function TvVotingScreen({ room }: TvVotingScreenProps) {
  const { clue, voteCount, expectedVoteCount, votingCards } = room
  const orderedCards = [...votingCards].sort(
    (first, second) => first.displayNumber - second.displayNumber,
  )

  return (
    <section className="tv-voting" aria-labelledby="tv-voting-title">
      <header className="tv-voting__header">
        <h2 className="tv-voting__heading" id="tv-voting-title">VOTACIÓN · LA PISTA</h2>
        <div className="tv-voting__clue" role="region" tabIndex={0} aria-label="Pista pública">
          {clue || 'Pista no disponible'}
        </div>
        <p className="tv-voting__instruction">
          Elige en tu teléfono la carta del narrador
        </p>
      </header>

      <ol className="tv-voting__cards" aria-label="Cartas de votación">
        {orderedCards.map((card) => (
          <li className="tv-voting__card" key={card.votingCardId}>
            <div className="tv-voting__art">Imagen no disponible</div>
            <span className="tv-voting__number">{card.displayNumber}</span>
          </li>
        ))}
      </ol>

      <div className="tv-voting__progress" role="status" aria-live="polite">
        <p className="tv-voting__count">{voteCount}/{expectedVoteCount} votos recibidos</p>
        <div className="tv-voting__circles" aria-hidden="true">
          {Array.from({ length: expectedVoteCount }, (_, index) => (
            <span
              className={`tv-voting__circle${index < voteCount ? ' tv-voting__circle--received' : ''}`}
              key={index}
            >
              {index < voteCount ? '✓' : null}
            </span>
          ))}
        </div>
      </div>
    </section>
  )
}

export default TvVotingScreen
