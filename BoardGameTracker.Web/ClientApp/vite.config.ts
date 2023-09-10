import {fileURLToPath, URL} from 'url';
import {defineConfig} from 'vite';
import mkcert from 'vite-plugin-mkcert';
import svgr from 'vite-plugin-svgr';

import react from '@vitejs/plugin-react';

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react(), svgr(), mkcert()],
  server: {
    https: true,
    port: 5173,
    strictPort: true,
    proxy: {
      '/api': {
        target: 'https://localhost:7178/',
        changeOrigin: true,
        secure: true
      }
    }
  }
})

