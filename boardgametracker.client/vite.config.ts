import { sentryVitePlugin } from "@sentry/vite-plugin";
import svgr from "vite-plugin-svgr";
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { tanstackRouter } from "@tanstack/router-plugin/vite";

const apiTarget = process.env.API_PROXY_TARGET || "http://localhost:6554/";

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
      sourcemaps: {
        filesToDeleteAfterUpload: ["./dist/**/*.map"],
      },
    }),
    svgr(),
    react(),
  ],
  resolve: {
    tsconfigPaths: true,
  },
  base: "/",

  server: {
    port: Number(process.env.PORT) || 5443,
    strictPort: true,
    proxy: {
      "/api": {
        target: apiTarget,
        changeOrigin: true,
        secure: false,
      },
      "/images/cover": {
        target: apiTarget,
        changeOrigin: true,
        secure: false,
      },
      "/images/profile": {
        target: apiTarget,
        changeOrigin: true,
        secure: false,
      },
    },
  },

  build: {
    sourcemap: "hidden",
  },
});
