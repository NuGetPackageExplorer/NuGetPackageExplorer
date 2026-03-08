import { defineConfig } from "@playwright/test";

const wasmTestPort = process.env.NPE_WASM_TEST_PORT ?? "4281";
const baseUrl = `http://127.0.0.1:${wasmTestPort}`;

export default defineConfig({
  testDir: "./tests/wasm-routing",
  fullyParallel: false,
  timeout: 90_000,
  expect: {
    timeout: 30_000
  },
  use: {
    baseURL: baseUrl,
    trace: "retain-on-failure",
    video: "retain-on-failure"
  },
  webServer: {
    command: "npm run swa:start:test",
    url: `${baseUrl}/packages`,
    reuseExistingServer: false,
    stdout: "pipe",
    stderr: "pipe",
    timeout: 600_000
  }
});
