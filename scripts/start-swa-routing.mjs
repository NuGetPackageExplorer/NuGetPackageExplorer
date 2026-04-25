import { spawn, spawnSync } from "node:child_process";

import {
  apiPath,
  ensureLocalToolsInstalled,
  ensureWorkspaceToolsInstalled,
  getFuncExecutable,
  getSwaExecutable,
  workspaceRoot
} from "./local-tooling.mjs";

const port = process.env.NPE_WASM_TEST_PORT ?? "4281";
const apiPort = process.env.NPE_API_TEST_PORT ?? "7071";
const configuration = process.env.NPE_WASM_TEST_CONFIGURATION ?? "Release";
const runtimeMode = process.env.NPE_WASM_TEST_RUNTIME_MODE;
const disableJiterpreter = process.env.NPE_WASM_TEST_DISABLE_JITERPRETER === "1";
const childEnvironment = {
  ...process.env,
  CI: process.env.CI ?? "1",
  FUNCTIONS_CORE_TOOLS_TELEMETRY_OPTOUT: process.env.FUNCTIONS_CORE_TOOLS_TELEMETRY_OPTOUT ?? "1"
};
const publishArgs = [
  "publish",
  "Uno/NuGetPackageExplorer/NuGetPackageExplorer.WinUI.csproj",
  "-f",
  "net10.0-browserwasm",
  "-c",
  configuration
];

if (runtimeMode) {
  publishArgs.push(`-p:WasmShellMonoRuntimeExecutionMode=${runtimeMode}`);
}

if (disableJiterpreter) {
  publishArgs.push("-p:WasmShellEnableJiterpreter=false");
}

const publishFolder = `artifacts/publish/NuGetPackageExplorer.WinUI/${configuration.toLowerCase()}_net10.0-browserwasm/wwwroot`;

const publish = spawnSync("dotnet", publishArgs, {
  cwd: process.cwd(),
  env: childEnvironment,
  stdio: "inherit",
  shell: process.platform === "win32"
});

if (publish.status !== 0) {
  process.exit(publish.status ?? 1);
}

ensureLocalToolsInstalled();
ensureWorkspaceToolsInstalled();

const func = spawn(
  getFuncExecutable(),
  [
    "start",
    "--port",
    apiPort,
    "--runtime",
    "default"
  ],
  {
    cwd: apiPath,
    env: childEnvironment,
    stdio: "inherit",
    shell: process.platform === "win32"
  }
);

const waitForApi = spawnSync(
  "node",
  [
    "scripts/wait-for-http.mjs",
    `http://127.0.0.1:${apiPort}/api/MsdlProxy?symbolkey=https://evil.invalid/a.pdb/abc/a.pdb`,
    "400",
    "90000"
  ],
  {
    cwd: workspaceRoot,
    env: childEnvironment,
    stdio: "inherit",
    shell: process.platform === "win32"
  }
);

if (waitForApi.status !== 0) {
  func.kill();
  process.exit(waitForApi.status ?? 1);
}

const swa = spawn(
  getSwaExecutable(),
  [
    "start",
    publishFolder,
    "--host",
    "127.0.0.1",
    "--port",
    port,
    "--api-devserver-url",
    `http://127.0.0.1:${apiPort}`,
    "--swa-config-location",
    "Uno/NuGetPackageExplorer/Platforms/WebAssembly/wwwroot"
  ],
  {
    cwd: workspaceRoot,
    env: childEnvironment,
    stdio: "inherit",
    shell: process.platform === "win32"
  }
);

func.on("exit", code => {
  if (swa.exitCode == null) {
    swa.kill();
  }

  process.exit(code ?? 0);
});

const forwardSignal = signal => {
  func.kill(signal);
  swa.kill(signal);
};

process.on("SIGINT", forwardSignal);
process.on("SIGTERM", forwardSignal);

swa.on("exit", code => {
  if (func.exitCode == null) {
    func.kill();
  }

  process.exit(code ?? 0);
});
