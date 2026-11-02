import { createReadStream, readFileSync, realpathSync, statSync } from 'node:fs'
import type { ServerResponse } from 'node:http'
import { dirname, isAbsolute, relative, resolve, sep } from 'node:path'
import { fileURLToPath } from 'node:url'
import react from '@vitejs/plugin-react'
import { defineConfig, type Plugin } from 'vite'

const projectRoot = dirname(fileURLToPath(import.meta.url))
const localCardAssetRoot = resolve(projectRoot, '../../docs/assets/cartas')
const localCardManifestPath = resolve(projectRoot, 'public/cards/manifest.v1.json')
const localCardManifest = JSON.parse(readFileSync(localCardManifestPath, 'utf8')) as {
  cards: Array<{ fileName: string }>
}
const localCardFileNames = new Set(localCardManifest.cards.map((card) => card.fileName))
const localCardRoute = '/__local-cards'
const localCardRoutePrefix = `${localCardRoute}/`

function isPathWithin(rootPath: string, candidatePath: string): boolean {
  const relativePath = relative(rootPath, candidatePath)

  return relativePath !== ''
    && relativePath !== '..'
    && !relativePath.startsWith(`..${sep}`)
    && !isAbsolute(relativePath)
}

function sendNotFound(response: ServerResponse): void {
  response.statusCode = 404
  response.setHeader('Content-Type', 'text/plain; charset=utf-8')
  response.end('Not found')
}

function localCardAssetsPlugin(): Plugin {
  return {
    name: 'local-card-assets',
    apply: 'serve',
    configureServer(server) {
      server.middlewares.use((request, response, next) => {
        const requestUrl = request.url ?? '/'
        const queryIndex = requestUrl.indexOf('?')
        const pathname = queryIndex === -1 ? requestUrl : requestUrl.slice(0, queryIndex)

        if (pathname !== localCardRoute && !pathname.startsWith(localCardRoutePrefix)) {
          return next()
        }

        if (request.method !== 'GET' && request.method !== 'HEAD') {
          response.statusCode = 405
          response.setHeader('Allow', 'GET, HEAD')
          return response.end()
        }

        if (pathname === localCardRoute) {
          return sendNotFound(response)
        }

        let fileName: string

        try {
          fileName = decodeURIComponent(pathname.slice(localCardRoutePrefix.length))
        } catch {
          return sendNotFound(response)
        }

        if (
          fileName.includes('/')
          || fileName.includes('\\')
          || !localCardFileNames.has(fileName)
        ) {
          return sendNotFound(response)
        }

        const candidatePath = resolve(localCardAssetRoot, fileName)

        if (!isPathWithin(localCardAssetRoot, candidatePath)) {
          return sendNotFound(response)
        }

        let imagePath: string
        let imageSize: number

        try {
          const realAssetRoot = realpathSync(localCardAssetRoot)
          imagePath = realpathSync(candidatePath)

          if (!isPathWithin(realAssetRoot, imagePath)) {
            return sendNotFound(response)
          }

          const imageStats = statSync(imagePath)

          if (!imageStats.isFile()) {
            return sendNotFound(response)
          }

          imageSize = imageStats.size
        } catch {
          return sendNotFound(response)
        }

        response.statusCode = 200
        response.setHeader('Content-Type', 'image/jpeg')
        response.setHeader('Content-Length', imageSize)
        response.setHeader('Cache-Control', 'no-cache')
        response.setHeader('X-Content-Type-Options', 'nosniff')

        if (request.method === 'HEAD') {
          return response.end()
        }

        const imageStream = createReadStream(imagePath)
        imageStream.on('error', () => {
          if (response.headersSent) {
            response.destroy()
          } else {
            sendNotFound(response)
          }
        })
        imageStream.pipe(response)
      })
    },
  }
}

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), localCardAssetsPlugin()],
  server: {
    allowedHosts: ['macbook-pro-de-reydel-2.local'],
    host: '0.0.0.0',
    port: 5173,
    strictPort: true,
  },
})
