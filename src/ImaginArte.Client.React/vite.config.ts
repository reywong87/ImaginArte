import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    allowedHosts: ['macbook-pro-de-reydel-2.local'],
    host: '0.0.0.0',
    port: 5173,
    strictPort: true,
  },
})
