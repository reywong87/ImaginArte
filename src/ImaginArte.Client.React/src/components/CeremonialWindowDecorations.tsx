import './CeremonialWindowDecorations.css'

const CORNERS = ['top-left', 'top-right', 'bottom-left', 'bottom-right'] as const

function CeremonialWindowDecorations() {
  return (
    <span className="ceremonial-window-decorations" aria-hidden="true">
      <span className="ceremonial-window-decorations__texture" />
      <span className="ceremonial-window-decorations__inner-frame" />
      {CORNERS.map((corner) => (
        <span
          className={`ceremonial-window-decorations__corner ceremonial-window-decorations__corner--${corner}`}
          key={corner}
        />
      ))}
    </span>
  )
}

export default CeremonialWindowDecorations
