import fs from "node:fs";
import path from "node:path";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";

const scriptPath = fileURLToPath(import.meta.url);
const scriptsDirectory = path.dirname(scriptPath);

export const workspaceRoot = path.resolve(scriptsDirectory, "..");
export const toolsPath = path.join(workspaceRoot, ".tools");
export const apiPath = path.join(workspaceRoot, "Uno", "Api");

export function getFuncExecutable() {
  return process.platform === "win32"
    ? path.join(toolsPath, "node_modules", ".bin", "func.cmd")
    : path.join(toolsPath, "node_modules", ".bin", "func");
}

export function ensureLocalToolsInstalled() {
  if (fs.existsSync(getFuncExecutable())) {
    return;
  }

  const npmExecutable = process.platform === "win32" ? "npm.cmd" : "npm";
  const install = spawnSync(
    npmExecutable,
    ["--prefix", toolsPath, "ci", "--no-audit", "--no-fund"],
    {
      cwd: workspaceRoot,
      stdio: "inherit"
    }
  );

  if (install.status !== 0) {
    process.exit(install.status ?? 1);
  }
}
