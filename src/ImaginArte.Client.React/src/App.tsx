import { useEffect, useState } from 'react'
import {
  AnimatePresence,
  MotionConfig,
  motion,
  useReducedMotion,
} from 'motion/react'
import './App.css'
import TvHomeScreen from './screens/TvHomeScreen'
import TvLobbyScreen from './screens/TvLobbyScreen'

function getRoomCodeFromPathname(pathname: string): string | null {
  const match = pathname.match(/^\/tv\/([^/]+)\/?$/)

  if (!match) {
    return null
  }

  try {
    return decodeURIComponent(match[1])
  } catch {
    return null
  }
}

function App() {
  const [pathname, setPathname] = useState(() => window.location.pathname)
  const shouldReduceMotion = useReducedMotion()

  function navigateToRoom(roomCode: string) {
    const roomPath = `/tv/${encodeURIComponent(roomCode)}`
    window.history.pushState(null, '', roomPath)
    setPathname(roomPath)
  }

  useEffect(() => {
    function handlePopState() {
      setPathname(window.location.pathname)
    }

    window.addEventListener('popstate', handlePopState)

    return () => {
      window.removeEventListener('popstate', handlePopState)
    }
  }, [])

  const roomCode = getRoomCodeFromPathname(pathname)

  return (
    <MotionConfig reducedMotion="user">
      <div className="app-shell">
        <div className="app-shell__backdrop" aria-hidden="true" />
        <AnimatePresence initial={false} mode="wait">
          {roomCode ? (
            <motion.div
              animate={{ opacity: 1, y: 0, scale: 1 }}
              className="app-shell__screen"
              exit={shouldReduceMotion
                ? { opacity: 0 }
                : { opacity: 0, y: -24, scale: 0.99 }}
              initial={shouldReduceMotion
                ? { opacity: 0 }
                : { opacity: 0, y: 36, scale: 0.98 }}
              key={`tv-lobby-${roomCode}`}
              transition={{
                duration: shouldReduceMotion ? 0.12 : 0.48,
                ease: 'easeOut',
              }}
            >
              <TvLobbyScreen roomCode={roomCode} />
            </motion.div>
          ) : (
            <motion.div
              animate={{ opacity: 1, y: 0, scale: 1 }}
              className="app-shell__screen"
              exit={shouldReduceMotion
                ? { opacity: 0 }
                : { opacity: 0, y: -24, scale: 0.99 }}
              initial={shouldReduceMotion
                ? { opacity: 0 }
                : { opacity: 0, y: 36, scale: 0.98 }}
              key="tv-home"
              transition={{
                duration: shouldReduceMotion ? 0.12 : 0.48,
                ease: 'easeOut',
              }}
            >
              <TvHomeScreen onRoomCreated={navigateToRoom} />
            </motion.div>
          )}
        </AnimatePresence>
      </div>
    </MotionConfig>
  )
}

export default App
