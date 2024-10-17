import tsconfigPaths from 'vite-tsconfig-paths';
import svgr from 'vite-plugin-svgr';
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react-swc';

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [svgr(), tsconfigPaths(), react()],
  base: '/',
  server: {
    port: 5443,
    strictPort: true,
    proxy: {
      '/api': {
        target: 'http://localhost:6554/',
        changeOrigin: true,
        secure: false,
      },
      '/images': {
        target: 'http://localhost:6554/',
        changeOrigin: true,
        secure: false,
      },
    },
  },
});
