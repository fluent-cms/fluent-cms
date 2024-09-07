import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vitejs.dev/config/
export default defineConfig({
  base: '/admin/', // Set the base path for all assets
  plugins: [react()]
})
