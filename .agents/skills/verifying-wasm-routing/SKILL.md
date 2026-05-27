---
name: verifying-wasm-routing
description: Use when changing or validating NuGet Package Explorer browser routing, deep links, SWA rewrites, canonical URLs, or package/version entry behavior.
---

# Verifying WASM Routing

## Overview

The public route contract is small and strict. Keep user-facing URLs canonical and treat Uno's published `package_*` paths as internal asset locations only.

## Route Contract

- `/` lands on `/packages`
- `/packages` shows landing/search
- `/packages?q=uno` preserves search behavior
- `/packages/{id}` resolves to a package view
- `/packages/{id}/{version}` resolves to that exact package version
- `/packages/{id}/{version}/...extra` is invalid and should not open a package

## Canonical URL Rules

- Public URLs must stay under `/packages/...`
- `package_*` must never appear in the user-facing URL
- `package_*` may appear in internal published asset paths only

## Local Verification

- Publish the WASM app:
  - `dotnet publish Uno/NuGetPackageExplorer/NuGetPackageExplorer.WinUI.csproj -f net10.0-browserwasm -c Release`
- Run the authoritative browser regression:
  - `npx playwright test tests/wasm-routing/deep-links.spec.ts`
- Run the UI flow if search/navigation behavior changed:
  - `npm run verify:swa-ui-flow`

## Files to Check

- `Uno/NuGetPackageExplorer/App.xaml.cs`
- `Uno/NuGetPackageExplorer/Helpers/ApplicationHelper.cs`
- `Core/DeepLinking/WasmPackageRouteParser.cs`
- `Uno/NuGetPackageExplorer/Platforms/WebAssembly/wwwroot/staticwebapp.config.json`
- `tests/wasm-routing/deep-links.spec.ts`

## SWA Expectations

- `navigationFallback` must preserve SPA routing without rewriting real package assets to HTML
- `_framework` and asset extensions stay excluded from fallback
- Do not widen routing rules in a way that makes `/package_*` a valid public route

## Preview Validation

- Validate the current PR preview host after checks pass
- If the preview host still shows old behavior, unregister the service worker and clear site data
- Re-check root `/`, search `/packages?q=...`, and a versioned package URL
