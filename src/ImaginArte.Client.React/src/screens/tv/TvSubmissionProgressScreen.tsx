import CeremonialWindowDecorations from '../../components/CeremonialWindowDecorations'
import type { RoomStateResponse } from '../../models/roomState'
import './TvSubmissionProgressScreen.css'

interface TvSubmissionProgressScreenProps {
  room: RoomStateResponse
}

function TvSubmissionProgressScreen({ room }: TvSubmissionProgressScreenProps) {
  const { clue, submissionCount, expectedSubmissionCount } = room
  const remaining = expectedSubmissionCount - submissionCount
  const waitingMessage = remaining <= 0
    ? 'Todas las cartas recibidas'
    : remaining === 1
      ? 'Esperando la última carta…'
      : 'Esperando las cartas…'

  return (
    <section
      className="tv-lobby-panel tv-lobby-panel--submission-progress"
      aria-labelledby="tv-submission-progress-title"
    >
      <CeremonialWindowDecorations />
      <div className="tv-submission-progress__content">
        <h2 className="tv-submission-progress__heading" id="tv-submission-progress-title">
          PISTA PÚBLICA
        </h2>
        <div
          className="tv-submission-progress__clue"
          role="region"
          tabIndex={0}
          aria-label="Pista pública"
        >
          {clue || 'Pista no disponible'}
        </div>
        <p className="tv-submission-progress__instruction">
          Elige en tu teléfono una carta que pueda contar esta historia.
        </p>
        <div className="tv-submission-progress__divider" aria-hidden="true">✦</div>
        <div className="tv-submission-progress__status" role="status" aria-live="polite">
          <h3 className="tv-submission-progress__heading">CARTAS RECIBIDAS</h3>
          <div className="tv-submission-progress__summary">
            <p className="tv-submission-progress__count" aria-label={`${submissionCount} de ${expectedSubmissionCount} cartas recibidas`}>
              {submissionCount} / {expectedSubmissionCount}
            </p>
            <div className="tv-submission-progress__circles" aria-hidden="true">
              {Array.from({ length: expectedSubmissionCount }, (_, index) => (
                <span
                  className={`tv-submission-progress__circle${index < submissionCount ? ' tv-submission-progress__circle--received' : ''}`}
                  key={index}
                >
                  {index < submissionCount ? '✓' : null}
                </span>
              ))}
            </div>
          </div>
          <p className="tv-submission-progress__message">{waitingMessage}</p>
        </div>
      </div>
    </section>
  )
}

export default TvSubmissionProgressScreen
