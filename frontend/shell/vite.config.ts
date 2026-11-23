import react from "@vitejs/plugin-react";
import { defineConfig } from "vitest/config";

const apiTarget = [
  "http",
  "://",
  "127.0.0.1",
  ":7001",
].join("");

export default defineConfig({
  plugins: [
    react(),
  ],

  server: {
    host: "127.0.0.1",
    port: 5173,
    strictPort: true,

    proxy: {
      "/api": {
        target: apiTarget,
        changeOrigin: true,
        secure: false,
      },
    },
  },

  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: [
      "./src/test/setup.ts",
    ],
    css: true,

    include: [
      "src/**/*.test.{ts,tsx}",
      "src/**/*.spec.{ts,tsx}",
    ],

    exclude: [
      "e2e/**",
      "node_modules/**",
      "dist/**",
      "playwright-report/**",
      "test-results/**",
      "blob-report/**",
    ],
  },
});