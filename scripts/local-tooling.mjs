import fs from "node:fs";
import path from "node:path";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";

const scriptPath = fileURLToPath(import.meta.url);
const scriptsDirectory = path.dirname(scriptPath);

export const workspaceRoot = path.resolve(scriptsDirectory, "..");
export const toolsPath = path.join(workspaceRoot, ".tools");
export const apiPath = path.join(workspaceRoot, "Uno", "Api");

function getExecutable(root, name) {
  return process.platform === "win32"
    ? path.join(root, "node_modules", ".bin", `${name}.cmd`)
    : path.join(root, "node_modules", ".bin", name);
}

export function getFuncExecutable() {
  return getExecutable(toolsPath, "func");
}

export function getSwaExecutable() {
  return getExecutable(workspaceRoot, "swa");
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

export function ensureWorkspaceToolsInstalled() {
  if (fs.existsSync(getSwaExecutable())) {
    return;
  }

  const npmExecutable = process.platform === "win32" ? "npm.cmd" : "npm";
  const install = spawnSync(
    npmExecutable,
    ["ci", "--no-audit", "--no-fund"],
    {
      cwd: workspaceRoot,
      stdio: "inherit"
    }
  );

  if (install.status !== 0) {
    process.exit(install.status ?? 1);
  }
}
