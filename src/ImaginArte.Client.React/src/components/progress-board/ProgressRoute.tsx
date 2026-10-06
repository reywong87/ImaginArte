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
  return (
    <line
      className="progress-route__connector"
      x1={start.x}
      y1={start.y}
      x2={end.x}
      y2={end.y}
    />
  )
}

function ProgressRouteNode({ number, point }: ProgressRouteNodeProps) {
  return (
    <g className="progress-route__node">
      <circle
        className="progress-route__node-circle"
        cx={point.x}
        cy={point.y}
        r="29.5"
      />
      <circle
        className="progress-route__node-inner-ring"
        cx={point.x}
        cy={point.y}
        r="24.5"
      />
      <text
        className="progress-route__node-number"
        x={point.x}
        y={point.y}
        dominantBaseline="central"
        textAnchor="middle"
      >
        {number}
      </text>
    </g>
  )
}

export default function ProgressRoute({ points }: ProgressRouteProps) {
  return (
    <svg
      className="progress-route"
      viewBox={`0 0 ${PROGRESS_ROUTE_VIEWBOX.width} ${PROGRESS_ROUTE_VIEWBOX.height}`}
      preserveAspectRatio="xMidYMid slice"
    >
      <g className="progress-route__connectors">
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
      </g>
      <g className="progress-route__nodes">
        {points.map((point, index) => {
          const number = index + 1

          return <ProgressRouteNode key={number} number={number} point={point} />
        })}
      </g>
    </svg>
  )
}
