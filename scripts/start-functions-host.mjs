import { spawn } from "node:child_process";

import {
  apiPath,
  ensureLocalToolsInstalled,
  getFuncExecutable
} from "./local-tooling.mjs";

const port = process.env.NPE_API_TEST_PORT ?? process.argv[2] ?? "7071";

ensureLocalToolsInstalled();

const func = spawn(
  getFuncExecutable(),
  [
    "start",
    "--port",
    port
  ],
  {
    cwd: apiPath,
    stdio: "inherit",
    shell: process.platform === "win32"
  }
);

const forwardSignal = signal => {
  func.kill(signal);
};

process.on("SIGINT", forwardSignal);
process.on("SIGTERM", forwardSignal);

func.on("exit", code => {
  process.exit(code ?? 0);
});
