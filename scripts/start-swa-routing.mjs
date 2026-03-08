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
const publishArgs = [
  "publish",
  "Uno/NuGetPackageExplorer/NuGetPackageExplorer.WinUI.csproj",
  "-f",
  "net10.0-browserwasm",
  "-c",
  "Release"
];

const publish = spawnSync("dotnet", publishArgs, {
  cwd: process.cwd(),
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
    apiPort
  ],
  {
    cwd: apiPath,
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
    "artifacts/publish/NuGetPackageExplorer.WinUI/release_net10.0-browserwasm/wwwroot",
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
