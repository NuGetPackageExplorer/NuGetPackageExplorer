import { defineConfig } from "@playwright/test";

export default defineConfig({
  testDir: "./tests/wasm-routing",
  fullyParallel: false,
  timeout: 90_000,
  expect: {
    timeout: 30_000
  },
  use: {
    baseURL: "http://127.0.0.1:4281",
    trace: "retain-on-failure",
    video: "retain-on-failure"
  },
  webServer: {
    command: "npm run swa:start:test",
    url: "http://127.0.0.1:4281/packages",
    reuseExistingServer: false,
    stdout: "pipe",
    stderr: "pipe",
    timeout: 600_000
  }
});
