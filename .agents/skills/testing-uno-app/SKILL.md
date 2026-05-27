---
name: testing-uno-app
description: Use when validating the NuGet Package Explorer Uno app locally across browser WASM, Uno desktop, WPF desktop, and the SWA-backed routing path.
---

# Testing Uno App

## Overview

Use the repo's real validation paths. For browser work, the authoritative local route path is SWA plus the Functions host, not a plain static server.

## When to Use

- Verifying Uno WASM behavior after code changes
- Checking WPF, Uno desktop, and browser builds together
- Validating deep links, search routes, reload behavior, or SWA preview parity
- Reproducing build or runtime problems seen in `Build Channel_WebAssembly`, `Store`, or `UnoSkia`

## Core Workflow

1. Build the desktop path you touched.
2. Publish the browser path.
3. Run the SWA-backed browser regression.
4. Run the targeted UI-flow verification if search/navigation changed.
5. If release packaging changed, run the matching release-channel build locally.

## Commands

- WPF/solution build:
  - `dotnet build NuGetPackageExplorer.slnx -c Release`
- Store/WAP build:
  - `MSBuild.exe PackageExplorer.Package/PackageExplorer.Package.wapproj /restore /p:Configuration=Release /p:ReleaseChannel=Store /p:AppxPackageDir="<repo>\\artifacts\\Store\\" /m:1 /clp:ErrorsOnly`
- Nightly/WAP build:
  - `MSBuild.exe PackageExplorer.Package/PackageExplorer.Package.wapproj /restore /p:Configuration=Release /p:ReleaseChannel=Nightly /p:AppxPackageDir="<repo>\\artifacts\\Nightly\\" /m:1 /clp:ErrorsOnly`
- Uno desktop publish:
  - `dotnet publish Uno/NuGetPackageExplorer/NuGetPackageExplorer.WinUI.csproj -f net10.0-desktop -c Release`
- Uno WASM publish:
  - `dotnet publish Uno/NuGetPackageExplorer/NuGetPackageExplorer.WinUI.csproj -f net10.0-browserwasm -c Release`
- Functions publish:
  - `dotnet publish Uno/Api/Api.csproj -c Release`
- SWA browser regression:
  - `npx playwright test tests/wasm-routing/deep-links.spec.ts`
- SWA UI-flow verification:
  - `npm run verify:swa-ui-flow`

## SWA Rules

- For the fully wired local browser path, use Playwright `webServer` / `npx playwright test ...` or `npm run swa:start:test`.
- `npm run swa:start` only starts the SWA emulator; run `npm run api:start` first when using it manually.
- `npm run api:wait` can be used to confirm the Functions host is ready before launching SWA manually.
- Canonical app URLs are `/packages/...`; `package_*` is internal-only.
- If preview behavior looks stale, clear the site data/service worker before concluding the code is wrong.

## Deep-Link Checks

- `/packages/{id}`
- `/packages/{id}/{version}`
- `/packages?q=...`
- invalid extra segments fall back cleanly
- hard refresh on deep links
- direct entry from a clean browser session

## Common Mistakes

- Using a plain static file server instead of SWA
- Treating `/package_*` as a valid public route
- Trusting a preview host without clearing old service worker state
- Running only the browser build when the change also affects Store/WPF packaging
