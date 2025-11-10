import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react-swc';
import tsconfigPaths from 'vite-tsconfig-paths';
import svgr from 'vite-plugin-svgr';

export default defineConfig({
  plugins: [svgr(), tsconfigPaths(), react()],
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
