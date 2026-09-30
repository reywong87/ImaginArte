import { useEffect, useState } from 'react'
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
    <main className="app-shell">
      {roomCode ? (
        <TvLobbyScreen roomCode={roomCode} />
      ) : (
        <TvHomeScreen onRoomCreated={navigateToRoom} />
      )}
    </main>
  )
}

export default App
