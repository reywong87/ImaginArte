import { useEffect, useState } from 'react'
import {
  AnimatePresence,
  MotionConfig,
  motion,
  useReducedMotion,
} from 'motion/react'
import './App.css'
import MobileJoinScreen from './screens/mobile/MobileJoinScreen'
import TvHomeScreen from './screens/tv/TvHomeScreen'
import TvLobbyScreen from './screens/tv/TvLobbyScreen'

type AppRoute =
  | { type: 'home' }
  | { type: 'tv-lobby'; roomCode: string }
  | { type: 'mobile-join'; roomCode: string }

function getRouteFromPathname(pathname: string): AppRoute {
  const tvMatch = pathname.match(/^\/tv\/([^/]+)\/?$/)

  if (tvMatch) {
    try {
      return { type: 'tv-lobby', roomCode: decodeURIComponent(tvMatch[1]) }
    } catch {
      return { type: 'home' }
    }
  }

  const joinMatch = pathname.match(/^\/join(?:\/([^/]+))?\/?$/)

  if (!joinMatch) {
    return { type: 'home' }
  }

  if (!joinMatch[1]) {
    return { type: 'mobile-join', roomCode: '' }
  }

  try {
    return { type: 'mobile-join', roomCode: decodeURIComponent(joinMatch[1]) }
  } catch {
    return { type: 'mobile-join', roomCode: '' }
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

  const route = getRouteFromPathname(pathname)
  const screenKey = route.type === 'tv-lobby'
    ? `tv-lobby-${route.roomCode}`
    : route.type === 'mobile-join'
      ? `mobile-join-${route.roomCode}`
      : 'tv-home'

  return (
    <MotionConfig reducedMotion="user">
      <div className="app-shell">
        <div className="app-shell__backdrop" aria-hidden="true" />
        <AnimatePresence initial={false} mode="wait">
          <motion.div
            animate={{ opacity: 1, y: 0, scale: 1 }}
            className="app-shell__screen"
            exit={shouldReduceMotion
              ? { opacity: 0 }
              : { opacity: 0, y: -24, scale: 0.99 }}
            initial={shouldReduceMotion
              ? { opacity: 0 }
              : { opacity: 0, y: 36, scale: 0.98 }}
            key={screenKey}
            transition={{
              duration: shouldReduceMotion ? 0.12 : 0.48,
              ease: 'easeOut',
            }}
          >
            {route.type === 'tv-lobby' ? (
              <TvLobbyScreen roomCode={route.roomCode} />
            ) : route.type === 'mobile-join' ? (
              <MobileJoinScreen roomCode={route.roomCode} />
            ) : (
              <TvHomeScreen onRoomCreated={navigateToRoom} />
            )}
          </motion.div>
        </AnimatePresence>
      </div>
    </MotionConfig>
  )
}

export default App
