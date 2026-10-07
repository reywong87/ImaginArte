import {
  PROGRESS_ROUTE_VIEWBOX,
  type ProgressRoutePoint,
} from '../../models/progressRouteLayouts'
import './ProgressRoute.css'

interface ProgressRouteProps {
  points: readonly ProgressRoutePoint[]
}

interface ProgressRouteConnectorProps {
  start: ProgressRoutePoint
  end: ProgressRoutePoint
}

interface ProgressRouteNodeProps {
  number: number
  point: ProgressRoutePoint
}

function ProgressRouteConnector({ start, end }: ProgressRouteConnectorProps) {
  const deltaX = end.x - start.x
  const deltaY = end.y - start.y
  const length = Math.hypot(deltaX, deltaY)
  const angle = Math.atan2(deltaY, deltaX) * (180 / Math.PI)
  const midpointX = (start.x + end.x) / 2
  const midpointY = (start.y + end.y) / 2

  return (
    <span
      className="progress-route__connector"
      aria-hidden="true"
      style={{
        left: `${(midpointX / PROGRESS_ROUTE_VIEWBOX.width) * 100}%`,
        top: `${(midpointY / PROGRESS_ROUTE_VIEWBOX.height) * 100}%`,
        width: `${(length / PROGRESS_ROUTE_VIEWBOX.width) * 100}%`,
        transform: `translate(-50%, -50%) rotate(${angle}deg)`,
      }}
    />
  )
}

function ProgressRouteNode({ number, point }: ProgressRouteNodeProps) {
  return (
    <span
      className="progress-route__node"
      role="listitem"
      aria-label={`Posición ${number}`}
      style={{
        left: `${(point.x / PROGRESS_ROUTE_VIEWBOX.width) * 100}%`,
        top: `${(point.y / PROGRESS_ROUTE_VIEWBOX.height) * 100}%`,
      }}
    >
      {number}
    </span>
  )
}

export default function ProgressRoute({ points }: ProgressRouteProps) {
  return (
    <div
      className="progress-route"
      role="list"
      aria-label={`Recorrido de progreso con ${points.length} posiciones numeradas`}
    >
      {points.slice(0, -1).map((start, index) => {
        const end = points[index + 1]

        if (!end) {
          return null
        }

        return (
          <ProgressRouteConnector
            key={`connector-${index + 1}-${index + 2}`}
            start={start}
            end={end}
          />
        )
      })}
      {points.map((point, index) => {
        const number = index + 1

        return (
          <ProgressRouteNode
            key={number}
            number={number}
            point={point}
          />
        )
      })}
    </div>
  )
}
