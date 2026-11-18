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
    exclude: ['**/node_modules/**', 'e2e/**'],
    coverage: {
      include: ['src/**/*.{ts,tsx}'],
      exclude: ['**/main.tsx', '**/App.tsx', '**/routeTree.gen.ts', '**/api.generated.ts', '**/vite-env.d.ts'],
      reporter: ['lcov', 'text-summary'],
      thresholds: {
        autoUpdate: true,
        statements: 88.54,
        branches: 86.76,
        functions: 82.23,
        lines: 88.87,
      },
    },
    reporters: ['default', ['vitest-sonar-reporter', { outputFile: 'coverage/sonar-report.xml' }]],
  },
});
