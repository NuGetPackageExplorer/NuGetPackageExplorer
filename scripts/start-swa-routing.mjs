import { spawn, spawnSync } from "node:child_process";

const port = process.env.NPE_WASM_TEST_PORT ?? "4281";
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

const swa = spawn(
  "npx",
  [
    "--yes",
    "@azure/static-web-apps-cli",
    "start",
    "artifacts/publish/NuGetPackageExplorer.WinUI/release_net10.0-browserwasm/wwwroot",
    "--host",
    "127.0.0.1",
    "--port",
    port
  ],
  {
    cwd: process.cwd(),
    stdio: "inherit",
    shell: process.platform === "win32"
  }
);

const forwardSignal = signal => {
  swa.kill(signal);
};

process.on("SIGINT", forwardSignal);
process.on("SIGTERM", forwardSignal);

swa.on("exit", code => {
  process.exit(code ?? 0);
});
