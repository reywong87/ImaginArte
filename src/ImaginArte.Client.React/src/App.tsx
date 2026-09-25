import { useEffect, useState } from 'react'
import './App.css'
import TvHomeScreen from './screens/TvHomeScreen'
import TvLobbyPlaceholder from './screens/TvLobbyPlaceholder'

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
        <TvLobbyPlaceholder roomCode={roomCode} />
      ) : (
        <TvHomeScreen />
      )}
    </main>
  )
}

export default App
