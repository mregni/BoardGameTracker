import {defineConfig} from 'vite';
import svgr from 'vite-plugin-svgr';

import react from '@vitejs/plugin-react-swc';

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react(), svgr()],
  base: '/',
  server: {
    port: 5443,
    strictPort: true,
    proxy: {
      '/api': {
        target: 'http://localhost:6554/',
        changeOrigin: true,
        secure: false
      },
      '/images': {
        target: 'http://localhost:6554/',
        changeOrigin: true,
        secure: false
      }
    }
  }
})
