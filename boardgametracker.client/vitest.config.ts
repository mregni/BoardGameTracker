import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
import svgr from 'vite-plugin-svgr';

export default defineConfig({
  plugins: [svgr(), react()],
  resolve: {
    tsconfigPaths: true,
  },
  test: {
    environment: 'jsdom',
    setupFiles: './src/test/setup.ts',
    css: true,
    coverage: {
      include: ['src/**/*.{ts,tsx}'],
      exclude: ['**/main.tsx', '**/App.tsx', '**/routeTree.gen.ts', '**/vite-env.d.ts'],
      reporter: ['lcov'],
    },
    reporters: ['default', ['vitest-sonar-reporter', { outputFile: 'coverage/sonar-report.xml' }]],
  },
});
