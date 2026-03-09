---
name: building-release-channels
description: Use when reproducing NuGet Package Explorer Azure DevOps release-channel builds locally or when deciding which exact local build command matches Zip, Store, Choco, UnoSkia, or WebAssembly.
---

# Building Release Channels

## Overview

This repo does not have one universal local build command. Match the local command to the channel you are trying to reproduce.

## Channel Mapping

- `Store` / `Nightly`
  - `PackageExplorer.Package/PackageExplorer.Package.wapproj`
  - Use Visual Studio `MSBuild.exe`
- `Zip` / `Choco`
  - `PackageExplorer/NuGetPackageExplorer.csproj`
  - Use `dotnet publish`
- `UnoSkia`
  - `Uno/NuGetPackageExplorer/NuGetPackageExplorer.WinUI.csproj -f net10.0-desktop`
- `WebAssembly`
  - `Uno/NuGetPackageExplorer/NuGetPackageExplorer.WinUI.csproj -f net10.0-browserwasm`
  - plus `Uno/Api/Api.csproj`

## Local Commands

- Store/WAP:
  - `MSBuild.exe PackageExplorer.Package/PackageExplorer.Package.wapproj /restore /p:Configuration=Release /p:AppxPackageDir="<repo>\\artifacts\\Store\\" /m:1 /clp:ErrorsOnly`
- Zip/desktop publish:
  - `dotnet publish PackageExplorer/NuGetPackageExplorer.csproj -c Release /p:PublishProfile=Properties/PublishProfiles/WinX64.pubxml`
- Uno desktop:
  - `dotnet publish Uno/NuGetPackageExplorer/NuGetPackageExplorer.WinUI.csproj -f net10.0-desktop -c Release`
- WebAssembly:
  - `dotnet publish Uno/NuGetPackageExplorer/NuGetPackageExplorer.WinUI.csproj -f net10.0-browserwasm -c Release`
- Functions:
  - `dotnet publish Uno/Api/Api.csproj -c Release`

## Supporting Notes

- Browser routing validation must go through SWA, not a plain file server.
- If reproducing Azure DevOps `Build Channel_WebAssembly`, build both the WASM app and the API path.
- If reproducing `Build Channel_Store`, use the WAP command path; `dotnet build` is not equivalent.
- Binlogs should live under `artifacts/logs` when you are trying to compare with pipeline output.

## Common Mistakes

- Using `dotnet` instead of `MSBuild.exe` for the Store path
- Validating WebAssembly without publishing
- Ignoring the API build when reproducing SWA/WebAssembly behavior
- Assuming a passing WASM build says anything about `Store` or `Zip`
