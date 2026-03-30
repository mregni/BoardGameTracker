import { sentryVitePlugin } from "@sentry/vite-plugin";
import svgr from "vite-plugin-svgr";
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { tanstackRouter } from "@tanstack/router-plugin/vite";

export default defineConfig({
  plugins: [
    tanstackRouter({
      target: "react",
      autoCodeSplitting: true,
      routeFileIgnorePattern: ".test.",
    }),
    sentryVitePlugin({
      org: "boardgametracker",
      project: "boardgametracker",
    }),
    svgr(),
    react(),
  ],
  resolve: {
    tsconfigPaths: true,
  },
  base: "/",

  server: {
    port: 5443,
    strictPort: true,
    proxy: {
      "/api": {
        target: "http://localhost:6554/",
        changeOrigin: true,
        secure: false,
      },
      "/images": {
        target: "http://localhost:6554/",
        changeOrigin: true,
        secure: false,
      },
    },
  },

  build: {
    sourcemap: true,
  },
});
