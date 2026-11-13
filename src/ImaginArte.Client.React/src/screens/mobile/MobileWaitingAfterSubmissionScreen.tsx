import envelope from '../../assets/sobre.png'
import './MobileWaitingAfterSubmissionScreen.css'

interface MobileWaitingAfterSubmissionScreenProps {
  variant: 'storyteller' | 'player' | 'neutral'
}

const waitingMessages = {
  storyteller: {
    heading: 'Tu carta está en camino',
    description: 'Esperamos las demás historias. No necesitas hacer nada más por ahora.',
  },
  player: {
    heading: 'Carta enviada',
    description: 'Esperamos a los demás. No necesitas hacer nada por ahora.',
  },
  neutral: {
    heading: 'Esperando a que continúe la partida',
    description: null,
  },
} as const

function MobileWaitingAfterSubmissionScreen({ variant }: MobileWaitingAfterSubmissionScreenProps) {
  const message = waitingMessages[variant]

  return (
    <section className="mobile-waiting" aria-labelledby="mobile-waiting-title">
      <div className="mobile-waiting__heading">
        <p className="mobile-waiting__eyebrow">{variant === 'neutral' ? 'Espera' : 'Enviado'}</p>
        <h1 id="mobile-waiting-title">{message.heading}</h1>
        {message.description && <p className="mobile-waiting__description">{message.description}</p>}
      </div>
      <img className="mobile-waiting__envelope" src={envelope} alt="" />
    </section>
  )
}

export default MobileWaitingAfterSubmissionScreen
